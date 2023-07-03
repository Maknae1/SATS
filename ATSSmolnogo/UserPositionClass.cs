using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATSSmolnogo
{
    public class UserPositionClass
    {
        //класс для красивого вывода информации о сотрудниках
        public long? Id { get; set; }
        public string Fullname { get; set; }
        public string Position { get; set; }
        public string Login { get; set; }
        public long? Parent { get; set; }
    }
}
