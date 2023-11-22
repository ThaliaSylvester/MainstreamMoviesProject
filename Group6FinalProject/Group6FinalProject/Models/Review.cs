﻿using System;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Group_6_Final_Project.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Group_6_Final_Project.Models
{
    public enum CustomerRating { One = 1, Two = 2, Three = 3, Four = 4, Five = 5 };

    public enum Status { Approved, NeedsReview };

    public class Review
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReviewID { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public CustomerRating Rating { get; set; }

        [Display(Name = "Description: ")]
        public string Description { get; set; }

        public Status Status { get; set; }

        public string UserID { get; set; }  // Foreign key for the Product

        public string MovieID { get; set; }  // Foreign key for the Product
        public Movie Movies { get; set; }
    }
}