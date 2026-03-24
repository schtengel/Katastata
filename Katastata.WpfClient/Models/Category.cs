
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Katastata.Models
{
    public class Category
    {
        public int Id { get; set; } //id категории
        public string Name { get; set; } //название категории
        public ICollection<Program>? Programs { get; set; } //программы в категории

        public static Category All => new Category { Id = -1, Name = "Все" };
    }
}
