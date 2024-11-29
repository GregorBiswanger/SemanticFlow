using System.ComponentModel.DataAnnotations;

namespace SemanticFlow.DemoWebApi.Workflow.DTOs;

public class Customer
{
    [Required]
    [Display(Description = "The full name of the customer.")]
    public string FullName { get; set; }

    [Required]
    [Display(Description = "The full address of the customer including street, house number, postal code, and city.")]
    public string Address { get; set; }

    [Required]
    [Display(Description = "The customer's phone number for contact in case of issues.")]
    public string PhoneNumber { get; set; }
}