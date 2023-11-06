﻿using Microsoft.AspNetCore.Identity;
using Group_6_Final_Project.Utilities;
using Group_6_Final_Project.DAL;
using Group_6_Final_Project.Models;
using Microsoft.SqlServer.Server;
using static System.Reflection.Metadata.BlobBuilder;

namespace Group_6_Final_Project.Seeding
{
    public static class SeedReviews
    {
        public static void
(AppDbContext db)
        {
            //Create a counter and a flag so we will know which record is causing problems
            Int32 intReviewsAdded = 0;
        String strReviewsTitle = "Begin";

        //Add the list of Reviewss
        List<Reviews> Reviewss = new List<Reviews>();
        Reviewss.Add(new Reviews
            {
                //Movie = "Jurassic Park",
                Rating = 5,
                //Customer = "Michelle",
                Description = "Best Movie I've ever seen.",
                Status = "Approved"
            });

            Reviewss.Add(new Reviews
            {
                //Movie = "The Secret Life of Walter Mitty",
                Rating = 4,
                //Customer = "Christopher",
                Description = "Not bad.",
                Status = "Approved"
            });

Reviewss.Add(new Reviews
{
    //Movie = "Jurassic Park",
    Rating = 5,
    //Customer = "Brad",
    Description = "Changed my life",
    Status = "Approved"
});

Reviewss.Add(new Reviews
{
    //Movie = "The Goonies",
    Rating = 5,
    //Customer = "Franco",
    Description = "Great family adventure Movie",
    Status = "Approved"
});

Reviewss.Add(new Reviews
{
    //Movie = "The Goonies",
    Rating = 4,
    //Customer = "Wendy",
    Description = "Good Movie",
    Status = "Approved"
});

Reviewss.Add(new Reviews
{
    //Movie = "The Goonies",
    Rating = 1,
    //Customer = "Lim",
    Description = "Worst thing I've ever seen",
    Status = "Approved"
});

Reviewss.Add(new Reviews
{
    //Movie = "The Goonies",
    Rating = 5,
    //Customer = "Brad",
    Description = "Reminded me of my summers in the NW",
    Status = "Approved"
});

Reviewss.Add(new Reviews
{
    //Movie = "The Goonies",
    Rating = 5,
    //Customer = "Shan",
    Description = "I love a good treasure hunt!",
    Status = "Needs Review"
});

Reviewss.Add(new Reviews
{
    //Movie = "The Goonies",
    Rating = 3,
    //Customer = "Jim Bob",
    Description = "Meh",
    Status = "Approved"
});

Reviewss.Add(new Reviews
{
    //Movie = "The Hobbit: The Battle of Five Armies",
    Rating = 4,
    //Customer = "Christopher",
    Description = "",
    Status = "Approved"
});

Reviewss.Add(new Reviews
{
    //Movie = "The Hobbit: The Battle of Five Armies",
    Rating = 4,
    //Customer = "Brad",
    Description = "",
    Status = "Approved"
});

Reviewss.Add(new Reviews
{
    //Movie = "The Hobbit: The Battle of Five Armies",
    Rating = 5,
    //Customer = "Michelle",
    Description = "",
    Status = "Approved"
});

Reviewss.Add(new Reviews
{
    //Movie = "The Hobbit: The Battle of Five Armies",
    Rating = 5,
    //Customer = "Franco",
    Description = "",
    Status = "Approved"
});

Reviewss.Add(new Reviews
{
    //Movie = "The Hobbit: The Battle of Five Armies",
    Rating = 1,
    //Customer = "Wendy",
    Description = "Too long",
    Status = "Needs Review"
});

Reviewss.Add(new Reviews
{
    //Movie = "The Hobbit: The Battle of Five Armies",
    Rating = 2,
    //Customer = "Lim",
    Description = "Did they really need to drag this out into its own Movie?",
    Status = "Needs Review"
});


try  //attempt to add or update the Reviews
{
    //loop through each of the Reviewss in the list
    foreach (Reviews ReviewsToAdd in Reviewss)
    {
        //set the flag to the current title to help with debugging
        strReviewsTitle = ReviewsToAdd.Title;

        //look to see if the Reviews is in the database - this assumes that no
        //two Reviewss have the same title
        Reviews dbReviews = db.Reviews.FirstOrDefault(b => b.Title == ReviewsToAdd.Title);

        //if the dbReviews is null, this title is not in the database
        if (dbReviews == null)
        {
            //add the Reviews to the database and save changes
            db.Reviews.Add(ReviewsToAdd);
            db.SaveChanges();

            //update the counter to help with debugging
            intReviewsAdded += 1;
        }
        else //dbReviews is not null - this title *is* in the database
        {
            //update all of the Reviews's properties
            //dbReviews.//Movie = ReviewsToAdd.//Movie;
            dbReviews.Rating = ReviewsToAdd.Rating;
            //dbReviews.//Customer = ReviewsToAdd.//Customer;
            dbReviews.Description = ReviewsToAdd.Description;
            dbReviews.Status = ReviewsToAdd.Status;

            //update the database and save the changes
            db.Update(dbReviews);
            db.SaveChanges();

            //update the counter to help with debugging
            intReviewsAdded += 1;
        } //this is the end of the else
    } //this is the end of the foreach lo op for the Reviewss
}//this is the end of the try block

catch (Exception ex)//something went wrong with adding or updating
{
    //Build a messsage using the flags we created
    String msg = "  Repositories added:" + intReviewsAdded + "; Error on " + strReviewsTitle;

    //throw the exception with the new message
    throw new InvalidOperationException(ex.Message + msg);
}
        }
    }