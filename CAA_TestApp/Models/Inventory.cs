﻿using System.ComponentModel.DataAnnotations;

namespace CAA_TestApp.Models
{
    public class Inventory : Auditable, IValidatableObject
    {
        public int ID { get; set; }
        
        public string ISBN { get; set; }

        [Display(Name = "Quantity")]
        [Required(ErrorMessage ="Quantity cannot be left blank.")]
        public int Quantity { get; set; }

        [Display(Name = "Notes")]
        [StringLength(50, ErrorMessage ="Notes cannot be longer than 50 characters.")]
        public string Notes { get; set; }

        [Display(Name = "Shelf")]
        public string ShelfOn { get; set; }

        [Required(ErrorMessage ="You must enter a cost for the inventory total.")]
        [Range(0.0, 10000.00, ErrorMessage = "Cost must be between $0 and $10,000.00")]
        [DataType(DataType.Currency)]
        public double Cost { get; set; }

        [Display(Name = "Date Received")]
        [Required(ErrorMessage = "Date received cannot be left blank.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime DateReceived { get; set; }

        [Display(Name = "Location")]
        [Required(ErrorMessage ="Select the location for this inventory record.")]
        public int LocationID { get; set; }

        [Display(Name = "Product")]
        [Required(ErrorMessage ="Select the product for this inventory record.")]
        public int ProductID { get; set; }

        [Display(Name = "Location")]
        public Location Location { get; set; }

        [Display(Name = "Product")]
        public Product Product { get; set; }

        public ItemPhoto ItemPhoto { get; set; }

        public ItemThumbnail ItemThumbnail { get; set; }
        
        public QrImage QRImage { get; set; }
        
        //Methods
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DateReceived > DateTime.Today)
            {
                yield return new ValidationResult("Date received cannot be in the future.", new[] { "DateReceived" });
            }
        }
    }
}
