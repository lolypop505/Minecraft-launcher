using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace launch.Views.Other
{
    public class ModPack : INotifyPropertyChanged
    {
        private string _name;
        private bool _isSelected;
        private ObservableCollection<Mod> _mods = new ObservableCollection<Mod>();

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Mod> Mods
        {
            get => _mods;
            set { _mods = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Mod : INotifyPropertyChanged
    {
        private string _name;
        private string _filePath;
        private bool _isEnabled;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ModSettingsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ModPack> _modPacks = new ObservableCollection<ModPack>();
        private ModPack _selectedModPack;
        private string _minecraftPath;

        public ObservableCollection<ModPack> ModPacks
        {
            get => _modPacks;
            set { _modPacks = value; OnPropertyChanged(); }
        }

        public ModPack SelectedModPack
        {
            get => _selectedModPack;
            set
            {
                if (_selectedModPack != value)
                {
                    _selectedModPack = value;
                    OnPropertyChanged();
                    LoadModsForSelectedPack();
                }
            }
        }

        public ICommand ApplyModsCommand { get; }
        public ICommand SaveModsCommand { get; }

        public ModSettingsViewModel(string dir)
        {
            _minecraftPath = dir;

            ApplyModsCommand = new RelayCommand(ApplyMods);
            SaveModsCommand = new RelayCommand(ResetMods);

            LoadModPacks();
        }

        private void LoadModPacks()
        {
            ModPacks.Clear();

            try
            {
                if (Directory.Exists(_minecraftPath))
                {
                    var directories = Directory.GetDirectories(_minecraftPath)
                        .Select(Path.GetFileName)
                        .Where(dir => !dir.StartsWith("."))
                        .ToList();

                    foreach (var dir in directories)
                    {
                        var modPack = new ModPack { Name = dir };
                        modPack.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(ModPack.IsSelected) && ((ModPack)s).IsSelected)
                            {
                                SelectedModPack = (ModPack)s;
                            }
                        };
                        ModPacks.Add(modPack);
                    }

                    if (ModPacks.Any())
                    {
                        SelectedModPack = ModPacks.First();
                        SelectedModPack.IsSelected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке версий: {ex.Message}");
            }
        }

        private void LoadModsForSelectedPack()
        {
            if (SelectedModPack == null) return;

            SelectedModPack.Mods.Clear();

            try
            {
                string modsPath = Path.Combine(_minecraftPath, SelectedModPack.Name, "mods");

                if (Directory.Exists(modsPath))
                {
                    var modFiles = Directory.GetFiles(modsPath, "*.jar")
                        .Concat(Directory.GetFiles(modsPath, "*.jar.disabled"));

                    foreach (var file in modFiles)
                    {
                        bool isEnabled = !file.EndsWith(".disabled");
                        string cleanName = Path.GetFileName(isEnabled ? file : file.Replace(".disabled", ""));

                        SelectedModPack.Mods.Add(new Mod
                        {
                            Name = cleanName,
                            FilePath = file,
                            IsEnabled = isEnabled
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке модов: {ex.Message}");
            }
        }

        private void ApplyMods()
        {
            if (SelectedModPack == null || SelectedModPack.Mods == null) return;

            try
            {
                foreach (var mod in SelectedModPack.Mods)
                {
                    string currentPath = mod.FilePath;
                    string desiredPath = mod.IsEnabled
                        ? currentPath.EndsWith(".disabled")
                            ? currentPath[..^9]
                            : currentPath
                        : currentPath.EndsWith(".disabled")
                            ? currentPath
                            : currentPath + ".disabled";

                    if (currentPath != desiredPath)
                    {
                        if (File.Exists(desiredPath)) File.Delete(desiredPath);
                        File.Move(currentPath, desiredPath);
                        mod.FilePath = desiredPath;
                    }
                }
                MessageBox.Show("Настройки модов успешно применены!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при применении настроек: {ex.Message}");
            }
        }

        private void ResetMods()
        {
            LoadModsForSelectedPack();
            MessageBox.Show("Настройки модов сброшены");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();
    }
}
