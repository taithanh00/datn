using System.ComponentModel.DataAnnotations;

namespace datn.Models
{
    public class Menu
    {
        public int Id { get; set; }
        public int DayOfWeek { get; set; }
        public DateOnly Date { get; set; }
        public MealType MealType { get; set; } // Sáng, Trưa, Xế
        [Required]
        public string DishName { get; set; } = string.Empty;
        public string? Ingredients { get; set; }
        public int? Calories { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<MenuOverride> MenuOverrides { get; set; } = new List<MenuOverride>();
    }

    public enum MealType
    {
        Breakfast = 0,
        Lunch = 1,
        Snack = 2
    }
}
