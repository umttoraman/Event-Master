using System.ComponentModel.DataAnnotations;

namespace EventMaster.Web.Models;

public class PaymentViewModel
{
    public Guid EventId { get; set; }

    [MinLength(1, ErrorMessage = "En az 1 koltuk seçiniz.")]
    public List<Guid> RoomSeatIds { get; set; } = new();

    public int TicketCount => RoomSeatIds?.Distinct().Count() ?? 0;

    public decimal UnitPrice { get; set; } = 100m; // demo display; server controls final price
    public decimal TotalPrice => UnitPrice * TicketCount;

    [Required(ErrorMessage = "Kart numarası zorunludur.")]
    [Display(Name = "Kart numarası")]
    [StringLength(19, MinimumLength = 12, ErrorMessage = "Kart numarası geçersiz.")]
    public string CardNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kart sahibi adı zorunludur.")]
    [Display(Name = "Kart sahibi")]
    [StringLength(128)]
    public string CardOwnerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Son kullanma tarihi zorunludur.")]
    [Display(Name = "Son kullanma (AA/YY)")]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Format: AA/YY (örn. 08/28)")]
    public string ExpireDate { get; set; } = string.Empty;

    [Required(ErrorMessage = "CVV zorunludur.")]
    [Display(Name = "CVV")]
    [RegularExpression(@"^\d{3}$", ErrorMessage = "CVV 3 haneli olmalıdır.")]
    public string Cvv { get; set; } = string.Empty;
}

