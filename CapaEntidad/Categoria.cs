using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{


  //IdCategoria  int primary key identity,
 // Descripcion varchar(100),
//Activo bit default 1,
//FechaRegistro datetime default getdate()
//)
    public class Categoria
    {
        public int IdCategoria { get; set; }
        public string Descripcion{ get; set; }
        public bool Activo { get; set; }
    }
}
