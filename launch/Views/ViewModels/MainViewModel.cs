using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;
using System.Windows.Input;

namespace launch.Views.ViewModels
{
    internal class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly Dictionary<Type, Page> _pageCache = new();
        private Page _currentPage;
        private Page _currentAdminPage;

        public Page CurrentPage
        {
            get => _currentPage ??= GetOrCreatePage<Home>();
            set => Set(ref _currentPage, value);
        }

        public Page CurrentAdminPage
        {
            get => _currentAdminPage ??= GetOrCreatePage<AdminHome>();
            set => Set(ref _currentAdminPage, value);
        }

        // Команды пользователя
        public ICommand OpenHomePage { get; }
        public ICommand OpenDownloadsPage{ get; }
        public ICommand OpenFriendsPage{ get; }
        public ICommand OpenHelpdeskPage { get; }
        public ICommand OpenNewsPage{ get; }
        public ICommand OpenSettingsPage { get; }
        public ICommand OpenProfilePage { get; }

        // Команды администратора
        public ICommand OpenAdminHomePage { get; }
        public ICommand OpenAdminDownloadsPage { get; }
        public ICommand OpenAdminNewsPage { get; }
        public ICommand OpenAdminHelpdeskPage { get; }
        public ICommand OpenAdminUsersPage { get; }
        public ICommand OpenAdminSubscriptionsPage { get; }

        public MainViewModel()
        {
            // Инициализация команд пользователя
            OpenHomePage = new RelayCommand(() => CurrentPage = GetOrCreatePage<Home>());
            OpenDownloadsPage = new RelayCommand(() => CurrentPage = GetOrCreatePage<Downloads>());
            OpenFriendsPage = new RelayCommand(() => CurrentPage = GetOrCreatePage<Friends>());
            OpenHelpdeskPage = new RelayCommand(() => CurrentPage = GetOrCreatePage<Helpdesk>());
            OpenNewsPage = new RelayCommand(() => CurrentPage = GetOrCreatePage<News>());
            OpenSettingsPage = new RelayCommand(() => CurrentPage = GetOrCreatePage<Settings>());
            OpenProfilePage = new RelayCommand(() => CurrentPage = GetOrCreatePage<Profile>());

            // Инициализация команд администратора
            OpenAdminHomePage = new RelayCommand(() => CurrentAdminPage = GetOrCreatePage<AdminHome>());
            OpenAdminDownloadsPage = new RelayCommand(() => CurrentAdminPage = GetOrCreatePage<AdminDownloads>());
            OpenAdminNewsPage = new RelayCommand(() => CurrentAdminPage = GetOrCreatePage<AdminNews>());
            OpenAdminHelpdeskPage = new RelayCommand(() => CurrentAdminPage = GetOrCreatePage<AdminHelpdesk>());
            OpenAdminUsersPage = new RelayCommand(() => CurrentAdminPage = GetOrCreatePage<AdminUsers>());
            OpenAdminSubscriptionsPage = new RelayCommand(() => CurrentAdminPage = GetOrCreatePage<AdminSubscriptions>());
        }

        private Page GetOrCreatePage<T>() where T : Page, new()
        {
            var type = typeof(T);
            if (!_pageCache.TryGetValue(type, out var page))
            {
                page = new T();
                _pageCache[type] = page;
            }
            return page;
        }

        public void Dispose()
        {
            foreach (var page in _pageCache.Values)
            {
                if (page is IDisposable disposable)
                    disposable.Dispose();
            }
            _pageCache.Clear();
        }
    }
}