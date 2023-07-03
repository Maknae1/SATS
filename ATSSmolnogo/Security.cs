using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATSSmolnogo
{
    public class Security
    {
        /// <summary>
        /// Типы
        /// </summary>
        public class Types
        {
            /// <summary>
            /// Типы объекта оргструктуры
            /// </summary>
            public enum OrganizationItemType
            {
                /// <summary>
                /// Должность
                /// </summary>
                Position,
                /// <summary>
                /// Отдел/Подразделение
                /// </summary>
                Department,
                /// <summary>
                /// Группа сотрудников
                /// </summary>
                OrganizationGroup
            }
        }

        /// <summary>
        /// Модели
        /// </summary>
        public class Models
        {
            /// <summary>
            /// Модель пользователя
            /// </summary>
            public class User
            {
                /// <summary>
                /// Идентификатор записи БД
                /// </summary>
                public long? Id { get; set; }
                /// <summary>
                /// Уникальный идентификатор
                /// </summary>
                public Guid? Uid { get; set; }
                /// <summary>
                /// Логин пользователя
                /// </summary>
                public string Login { get; set; }
                /// <summary>
                /// Фамилия Имя Отчество
                /// </summary>
                public string FullName { get { return !string.IsNullOrEmpty(MiddleName) ? string.Format("{0} {1} {2}", this.LastName, this.FirstName, this.MiddleName) : string.Format("{0} {1} {2}", this.LastName, this.FirstName, this.MiddleName); } }
                /// <summary>
                /// Имя
                /// </summary>
                public string FirstName { get; set; }
                /// <summary>
                /// Отчество
                /// </summary>
                public string MiddleName { get; set; }
                /// <summary>
                /// Фамилия
                /// </summary>
                public string LastName { get; set; }

            }

            public class OrganizationItem
            {
                /// <summary>
                /// Идентификатор записи БД
                /// </summary>
                public long? Id { get; set; }
                /// <summary>
                /// Уникальный идентификатор
                /// </summary>
                public Guid? Uid { get; set; }
                /// <summary>
                /// Объект родитель
                /// </summary>
                public long? Parent { get; set; }
                /// <summary>
                /// Наименование
                /// </summary>
                public string Name { get; set; }
                /// <summary>
                /// Пользователь
                /// </summary>
                public User User { get; set; }
                /// <summary>
                /// Пользователи
                /// </summary>
                public List<User> Users { get; set; }

                /// <summary>
                /// Тип объекта орг. структуры
                /// </summary>
                public Security.Types.OrganizationItemType orgItemType { get; set; }


                /// <summary>
                /// Получить пользователей на указанной должности
                /// </summary>
                /// <returns>Список пользователей</returns>
                public List<User> GetUsers()
                {
                    var usersToReturn = new List<User>();
                    if (User != null) usersToReturn.Add(User);

                    Users.ForEach(c => usersToReturn.Add(c));
                    return usersToReturn;
                }

            }

        }
    }
}
