using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ATSSmolnogo
{
    public partial class MainWindow : Window
    {
        public List<Security.Models.User> SystemUsers = new List<Security.Models.User>();
        public List<Security.Models.OrganizationItem> SystemPosition = new List<Security.Models.OrganizationItem>();
        public List<Security.Models.OrganizationItem> PositionsList = new List<Security.Models.OrganizationItem>(); //список должностей без отделов
        public List<UserPositionClass> users = new List<UserPositionClass>(); //список сотрудников для вывода
        public List<UserPositionClass> permanentUsersList = new List<UserPositionClass>(); //неизменяемый список сотрудников для удобства
        private Random random = new Random();

        public MainWindow()
        {
            InitializeComponent();
            //инициализация объектов
            sortComboBox.SelectionChanged += SortComboBoxSelectionChanged;
            filtrationComboBox.SelectionChanged += FiltrationComboBoxSelectionChanged;
            SystemUsers = _getBaseUsers();
            SystemPosition = _getBaseOrganizationItem();
            PositionsList = SystemPosition.Where(f => f.orgItemType == Security.Types.OrganizationItemType.Position).ToList();

            //костыль для номальной работы со списком должностей (так как рабоников отдела качества несколько, поле User в классе OrganizationItem у него пустое)
            foreach (var item in PositionsList)
            {
                if (item.Users != null)
                    item.User = item.Users[0];
            }
            permanentUsersList = GetUserPositionClassItems();
            LoadData();
        }

        //метод для получения цепочки руководителей по нажтию кнопки
        private void ChainOfSupervisorsButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            long? currentPositionId = (long?)(sender as Button).Tag;

            //исключение курирущего зам директора из цепочки руководителей
            int isPositionCorrect =  currentPositionId > 3 ? SystemPosition.Count(f => f.Id == currentPositionId) : 0;
            if(isPositionCorrect != 0)
            {
                var currentPosition = SystemPosition.First(f => f.Id == currentPositionId);
                List<UserPositionClass> supervisorNames = new List<UserPositionClass>(); //лист цепочки руководителей

                //генерация цепочки руководителей
                while (currentPosition.Parent != 2 && currentPosition.Parent != 3 && currentPosition.Parent != 1)
                {
                    //исключение отделов из цепочки руководителей
                    int isSuperVisorDepartment = PositionsList.Count(f => f.Id == currentPosition.Parent);

                    if (isSuperVisorDepartment == 0)
                    {
                        currentPosition = SystemPosition.First(f => f.Id == currentPosition.Parent);
                    }
                    else
                    {
                        //добавление подходящей должности в цепочку пользователей
                        var currentSupervisor = SystemPosition.First(f => f.Id == currentPosition.Parent);
                        supervisorNames.Add(new UserPositionClass
                        {
                            Id = currentSupervisor.Id,
                            Fullname = SystemUsers.First(f => f.Login == currentSupervisor.User.Login).FullName,
                            Position = currentSupervisor.Name
                        });
                        currentPosition = SystemPosition.First(f => f.Id == currentPosition.Parent);
                    }
                }
                //вывод курирцющего зам директора отдельным значением
                var currentDeputyDirector = SystemPosition.First(f => f.Id == currentPosition.Parent);
                deputyDirectorTextBlock.Text = $"Курирующий заместитель директора: {SystemUsers.First(f => f.Login == currentDeputyDirector.User.Login).FullName}";

                //добавление директора в цепочку руководителей
                var director = SystemPosition.First(f => f.Id == 1);
                supervisorNames.Add(new UserPositionClass
                {
                    Id = director.Id,
                    Fullname = SystemUsers.First(f => f.Login == director.User.Login).FullName,
                    Position = director.Name
                });

                //вывод цепочки на эеран
                chainOfSupervisorsListBox.Items = supervisorNames.Select(f => new 
                {
                    f.Fullname,
                    Position = $"({f.Position})"
                });
            }
            else
            {
                //вывод в случае выбора директора для составления цепочки руководителей
                if(currentPositionId == 1)
                    deputyDirectorTextBlock.Text = "Самый главный";
                else
                {
                    //составление цепочки руководителей для зам директора
                    List<UserPositionClass> supervisorNames = new List<UserPositionClass>();
                    var director = SystemUsers.First(f => f.Id == 2);
                    supervisorNames.Add(new UserPositionClass
                    {
                        Id = 1,
                        Fullname = director.FullName,
                        Login = director.Login,
                        Position = "Директор"
                    });

                    chainOfSupervisorsListBox.Items = supervisorNames.Select(f => new
                    {
                        f.Fullname,
                        Position = $"({f.Position})"
                    });
                    deputyDirectorTextBlock.Text = "";
                }
            }
        }

        //фильтрация сотрудников
        private void FiltrationComboBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (filtrationComboBox.SelectedIndex == 1)
                users = GetRandomUsersList();
            LoadData();
        }

        //сортировка
        private void SortComboBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            LoadData();
        }

        //загрузка данных
        public void LoadData()
        {
            SystemUsers = _getBaseUsers();
            SystemPosition = _getBaseOrganizationItem();
            PositionsList = SystemPosition.Where(f => f.orgItemType == Security.Types.OrganizationItemType.Position).ToList();
            
            foreach (var item in PositionsList)
            {
                if (item.Users != null)
                    item.User = item.Users[0];
            }
            users = permanentUsersList;

            //фильтрация и сортировка списка сотрудников по должностям
            switch (filtrationComboBox.SelectedIndex)
            {
                case 0:
                    users = GetRandomUser();
                    break;
                case 1:
                    if (sortComboBox.SelectedIndex == 0)
                        users = users.OrderBy(f => f.Parent)
                            .ToList();
                    else
                        users = users.OrderByDescending(f => f.Parent)
                            .ToList();
                    break;
            }

            //вывод всего этого мракобесия на экран
            usersListBox.Items = users.Select(f => new
            {
                f.Id,
                f.Fullname,
                f.Position,
                f.Login
            });
        }


        /// <summary>
        /// Генерирует список пользователей и должностей
        /// </summary>
        /// <returns></returns>
        public List<UserPositionClass> GetUserPositionClassItems()
        {
            var userPositions = new List<UserPositionClass>();
            foreach(var item in SystemUsers)
            {
                if(item.Login != "admin")
                {
                    int isPositionUnlucky = PositionsList.Count(f => f.User.Login == item.Login); //начало костыля для работников отдела качества

                    switch (isPositionUnlucky)
                    {
                        case 0:
                            //костыль для работников отдела качества (чтобы manage2 тоже выводился)
                            var unluckyPosition = PositionsList.First(f => f.Users != null);//должность "работника отдела качества", о которой поле User равно null
                            unluckyPosition.User = unluckyPosition.Users[1];//изменение manage1 на manage2
                            var currentPosition = PositionsList.First(f => f.User.Login == item.Login); 
                            userPositions.Add(new UserPositionClass
                            {
                                Id = currentPosition.Id,
                                Fullname = item.FullName,
                                Position = currentPosition.Name,
                                Login = currentPosition.User.Login,
                                Parent = currentPosition.Parent,
                            });
                            break;

                        case 1:
                            //поиск и добавление в список подходящего пользователя
                            var userPosition = PositionsList.First(f => f.User.Login == item.Login);
                            userPositions.Add(new UserPositionClass
                            {
                                Id = userPosition.Id,
                                Fullname = item.FullName,
                                Position = userPosition.Name,
                                Login = userPosition.User.Login,
                                Parent = userPosition.Parent != null? userPosition.Parent : 0
                            });
                            break;
                    }
                }
                else
                {
                    //добавление админа как отдельного пользователя
                    userPositions.Add(new UserPositionClass
                    {
                        Id = 0,
                        Fullname = $"{item.LastName} {item.FirstName}",
                        Login = item.Login,
                        Position = "admin",
                        Parent = 0
                    });
                }
            }
            return userPositions;
        }

        /// <summary>
        /// Генерирует должности
        /// </summary>
        /// <returns></returns>
        public List<Security.Models.OrganizationItem> _getBaseOrganizationItem()
        {
            var baseOrgItem = new List<Security.Models.OrganizationItem>();

            baseOrgItem.Add(new Security.Models.OrganizationItem() { Id = 1, orgItemType = Security.Types.OrganizationItemType.Position, Uid = System.Guid.NewGuid(), Parent = null, Name = "Директор", User = this.SystemUsers.First(c => c.Login == "gendir") });
            baseOrgItem.Add(new Security.Models.OrganizationItem() { Id = 2, orgItemType = Security.Types.OrganizationItemType.Position, Uid = System.Guid.NewGuid(), Parent = 1, Name = "Заместитель директора по тех. управлению", User = this.SystemUsers.First(c => c.Login == "kurzamtb") });
            baseOrgItem.Add(new Security.Models.OrganizationItem() { Id = 3, orgItemType = Security.Types.OrganizationItemType.Position, Uid = System.Guid.NewGuid(), Parent = 1, Name = "Заместитель директора по качеству", User = this.SystemUsers.First(c => c.Login == "kurzammb") });

            baseOrgItem.Add(new Security.Models.OrganizationItem() { Id = 4, orgItemType = Security.Types.OrganizationItemType.Department, Uid = System.Guid.NewGuid(), Parent = 2, Name = "Отдел Информационных Систем" });

            baseOrgItem.Add(new Security.Models.OrganizationItem() { Id = 6, orgItemType = Security.Types.OrganizationItemType.Position, Uid = System.Guid.NewGuid(), Parent = 4, Name = "Начальник Отдела Информационных Систем", User = this.SystemUsers.First(c => c.Login == "nach_ois") });
            baseOrgItem.Add(new Security.Models.OrganizationItem() { Id = 7, orgItemType = Security.Types.OrganizationItemType.Position, Uid = System.Guid.NewGuid(), Parent = 6, Name = "Программист", User = this.SystemUsers.First(c => c.Login == "dev1") });
            baseOrgItem.Add(new Security.Models.OrganizationItem() { Id = 8, orgItemType = Security.Types.OrganizationItemType.Position, Uid = System.Guid.NewGuid(), Parent = 6, Name = "Аналитик", User = this.SystemUsers.First(c => c.Login == "dev2") });

            baseOrgItem.Add(new Security.Models.OrganizationItem() { Id = 5, orgItemType = Security.Types.OrganizationItemType.Department, Uid = System.Guid.NewGuid(), Parent = 3, Name = "Отдел Качества" });

            baseOrgItem.Add(new Security.Models.OrganizationItem() { Id = 9, orgItemType = Security.Types.OrganizationItemType.Position, Uid = System.Guid.NewGuid(), Parent = 5, Name = "Начальник Отдела Качества", User = this.SystemUsers.First(c => c.Login == "nach_manage") });
            baseOrgItem.Add(new Security.Models.OrganizationItem() { Id = 10, orgItemType = Security.Types.OrganizationItemType.Position, Uid = System.Guid.NewGuid(), Parent = 9, Name = "Работник Отдела Качества", Users = this.SystemUsers.Where(c => c.Login == "manage2" || c.Login == "manage1").ToList() });
            return baseOrgItem;
        }

        /// <summary>
        /// Генерирует пользователей
        /// </summary>
        /// <returns></returns>
        private List<Security.Models.User> _getBaseUsers()
        {
            var baseUsers = new List<Security.Models.User>();
            baseUsers.Add(new Security.Models.User { Id = 1, Uid = System.Guid.NewGuid(), Login = "admin", LastName = "Администратор", FirstName = "Системы" });
            baseUsers.Add(new Security.Models.User { Id = 2, Uid = System.Guid.NewGuid(), Login = "gendir", LastName = "Андреев", FirstName = "Андрей", MiddleName = "Андреевич" });
            baseUsers.Add(new Security.Models.User { Id = 3, Uid = System.Guid.NewGuid(), Login = "kurzamtb", LastName = "Иванов", FirstName = "Иван", MiddleName = "Иванович" });
            baseUsers.Add(new Security.Models.User { Id = 4, Uid = System.Guid.NewGuid(), Login = "kurzammb", LastName = "Петрова", FirstName = "Александра", MiddleName = "Александровна" });

            baseUsers.Add(new Security.Models.User { Id = 5, Uid = System.Guid.NewGuid(), Login = "nach_ois", LastName = "Александров", FirstName = "Александр", MiddleName = "Александрович" });
            baseUsers.Add(new Security.Models.User { Id = 6, Uid = System.Guid.NewGuid(), Login = "dev1", LastName = "Попелышева", FirstName = "Анастасия", MiddleName = "Игоревна" });
            baseUsers.Add(new Security.Models.User { Id = 7, Uid = System.Guid.NewGuid(), Login = "dev2", LastName = "Романов", FirstName = "Александр", MiddleName = "Михайлович" });

            baseUsers.Add(new Security.Models.User { Id = 8, Uid = System.Guid.NewGuid(), Login = "nach_manage", LastName = "Романов", FirstName = "Александр", MiddleName = "Михайлович" });
            baseUsers.Add(new Security.Models.User { Id = 9, Uid = System.Guid.NewGuid(), Login = "manage1", LastName = "Романов", FirstName = "Александр", MiddleName = "Михайлович" });
            baseUsers.Add(new Security.Models.User { Id = 10, Uid = System.Guid.NewGuid(), Login = "manage2", LastName = "Петрова", FirstName = "Александра", MiddleName = "Александровна" });

            return baseUsers;
        }
        //случайный пользователь
        public List<UserPositionClass> GetRandomUser()
        {
            var prepList = new List<UserPositionClass>()
            {
                this.permanentUsersList[this.random.Next(this.permanentUsersList.Count)]
            };
            return prepList;
        }

        //список случайных пользователей
        public List<UserPositionClass> GetRandomUsersList()
        {
            var prepList = new List<UserPositionClass>();
            for (int i = 0; i < this.SystemUsers.Count; i++)
            {
                prepList.Add(this.permanentUsersList[this.random.Next(this.permanentUsersList.Count)]);
            }
            return prepList.Distinct().ToList();
        }
    }
}