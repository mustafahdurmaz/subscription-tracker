// Domain genelinde kullanılan enum tipleri.
// ServiceType: abonelik türü, SubscriptionStatus: abonelik aktiflik durumu,
// PaymentStatus: ödeme sonucu. Sayısal değerler stabil (DB'de int olarak tutulur).
namespace SubscriptionTracker.Api.Models.Entities;

public enum ServiceType
{
    Electricity = 0,
    Water = 1,
    Internet = 2,
    Gsm = 3,
    NaturalGas = 4,
    Other = 5
}

public enum SubscriptionStatus
{
    Active = 0,
    Inactive = 1
}

public enum PaymentStatus
{
    Success = 0,
    Failed = 1
}
