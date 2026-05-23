using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HrDepartment
{
    public class LookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public override string ToString() => Name;
    }
}
