using Azure.Core;
using Bookstore.src;
using BookStore.src;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;
using System.Runtime;

namespace BookStore
{
    

    internal class Program
    {
        public static Mutex? mutex { get; set; } = new Mutex();

        public static Semaphore semaphore { get; set; } = new Semaphore(2, 2);

        public static object locker = new object();
        static void Main(string[] args)
        {
            //Monitor
            //Thread t1 = new(AuthorsMonitorGeneration);
            //Thread t2 = new(AuthorsMonitorGeneration2);

            //t1.Start();
            //t2.Start();

            ////Mutex
            //Thread t3 = new(AuthorsMutexGeneration1);
            //t3.Name = "Mutex thread 1";

            //Thread t4 = new(AuthorsMutexGeneration2);
            //t4.Name = "Mutex thread 2";

            //t3.Start();
            //t4.Start();

            //Semaphore
            //Thread t5 = new Thread(AuthorsSemaphoreGeneration1);
            //t5.Name = "Semaphore Thread 1";
            //t5.Start();

            //Reading data using lock
            
            //for(int i = 0; i < 4; i++)
            //{
            //    Thread tr = new(PrintAuthors);
            //    tr.Name = $"Lock Thread {i}";
            //    tr.Start();
            //}

            //Reading data using Task

            TaskExample();

        }

        public static void TaskExample()
        {
            ContextFactory cf = new ContextFactory();
            using (var c = cf.CreateDbContext(new[] { "MyConnection" }))
            {
                var outer = Task.Factory.StartNew(() =>      // зовнішнє завдання
                {
                    Console.WriteLine("Outer task starting...");
                    foreach (Author a in c.Authors.ToList()) Console.WriteLine($"Task {Task.CurrentId}: {a.FirstName} {a.LastName}");

                    var inner = Task.Factory.StartNew(() =>  // вкладене завдання
                    {
                        Console.WriteLine($"Inner task({Task.CurrentId}) starting...");
                        Thread.Sleep(2000);
                        foreach (Author a in c.Authors.ToList()) Console.WriteLine($"Task {Task.CurrentId}: {a.FirstName} {a.LastName}");
                        Console.WriteLine("Inner task finished.");
                    }, TaskCreationOptions.AttachedToParent);
                });
                outer.Wait();
                Console.WriteLine("End of Main");


            }
        }
        public static void PrintAuthors()
        {
            ContextFactory cf = new ContextFactory();
            using (var c = cf.CreateDbContext(new[] { "MyConnection" }))
            {
                lock(locker)
                { 
                    foreach(Author a in c.Authors.ToList())
                    {
                        Console.WriteLine($"{Thread.CurrentThread.Name} : {a.FirstName} {a.LastName}");
                        Thread.Sleep(500);
                    }
                }
            }
        }

        public static void AuthorsMonitorGeneration()
        {
            ContextFactory cf = new ContextFactory();
            using (var c = cf.CreateDbContext(new[] { "MyConnection" }))
            {
                bool acquiredLock = false;
                try
                {
                    Monitor.Enter(c, ref acquiredLock);
                    c.Authors.Add(new Author { FirstName = "Pavlo", LastName = "Lacoste", CountryOfBirth = "France", DateOfBirth = DateTime.Now });
                    Console.WriteLine("Thread (1): First client added");
                    c.Authors.Add(new Author { FirstName = "Nancy", LastName = "Li", CountryOfBirth = "England", DateOfBirth = DateTime.Now });
                    Console.WriteLine("Thread (1): The second client added");
                    c.SaveChanges();
                }
                finally
                {
                    if (acquiredLock) Monitor.Exit(c);
                }
            }

        }
        
        public static void AuthorsMonitorGeneration2()
        {
            ContextFactory cf = new ContextFactory();
            using (var c = cf.CreateDbContext(new[] { "MyConnection" }))
            {
                bool acquiredLock = false;
                try
                {
                    Monitor.Enter(c, ref acquiredLock);
                    c.Authors.Add(new Author { FirstName = "James", LastName = "Verona", CountryOfBirth = "Italy", DateOfBirth = DateTime.Now });
                    Console.WriteLine("Thread (2): First client added");
                    c.Authors.Add(new Author { FirstName = "Maria", LastName = "Franko", CountryOfBirth = "Ukraine", DateOfBirth = DateTime.Now });
                    Console.WriteLine("Thread (2): The second client added");
                    c.SaveChanges();
                }
                finally
                {
                    if (acquiredLock) Monitor.Exit(c);
                }
            }

        }

        public static void AuthorsMutexGeneration1()
        {
            ContextFactory cf = new ContextFactory();
            using (var c = cf.CreateDbContext(new[] { "MyConnection" }))
            {
                mutex.WaitOne();
                c.Authors.Add(new Author { FirstName = "James", LastName = "Clear", CountryOfBirth = "USA", DateOfBirth = DateTime.Now });
                Console.WriteLine($"{Thread.CurrentThread.Name} : First client added");
                c.Authors.Add(new Author { FirstName = "Lucy", LastName = "Fernando", CountryOfBirth = "Sweden", DateOfBirth = DateTime.Now });
                Console.WriteLine($"{Thread.CurrentThread.Name} : The second client added");
                c.SaveChanges();
                mutex.ReleaseMutex();
            }
        }

        public static void AuthorsMutexGeneration2()
        {
            ContextFactory cf = new ContextFactory();
            using (var c = cf.CreateDbContext(new[] { "MyConnection" }))
            {
                mutex.WaitOne();
                c.Authors.Add(new Author { FirstName = "Micky", LastName = "Mouse", CountryOfBirth = "France", DateOfBirth = DateTime.Now });
                Console.WriteLine($"{Thread.CurrentThread.Name} : First client added");
                c.Authors.Add(new Author { FirstName = "Angela", LastName = "Loko", CountryOfBirth = "USA", DateOfBirth = DateTime.Now });
                Console.WriteLine($"{Thread.CurrentThread.Name} : The second client added");
                c.SaveChanges();
                mutex.ReleaseMutex();
            }
        }

        public static void AuthorsSemaphoreGeneration1()
        {
            ContextFactory cf = new ContextFactory();
            using (var c = cf.CreateDbContext(new[] { "MyConnection" }))
            { 
                semaphore.WaitOne();
                c.Authors.Add(new Author { FirstName = "Ferdinand", LastName = "Bavarskiy", CountryOfBirth = "Germany", DateOfBirth = DateTime.Now });
                Console.WriteLine($"{Thread.CurrentThread.Name} : First client added");
                c.Authors.Add(new Author { FirstName = "Tracy", LastName = "Trancher", CountryOfBirth = "USA", DateOfBirth = DateTime.Now });
                Console.WriteLine($"{Thread.CurrentThread.Name} : The second client added");
                c.SaveChanges();
                semaphore.Release();
            }
        }

        public static void Intersection(MyContext context  )
        {
            var books = context.Books.Where(a => a.Name.StartsWith("M")).Intersect(context.Books.Where(a => a.AuthorId % 2 != 0));

            foreach (var book in books) { Console.WriteLine(book.Name + " " + book.AuthorId); }
        }

        public static void Union(MyContext context)
        {
            var books = context.Books.Where(a => a.Name.StartsWith("M")).Union(context.Books.Where(a => a.AuthorId % 2 == 0));

            foreach (var book in books) { Console.WriteLine(book.Name + " " + book.AuthorId); }
        }

        public static void Except(MyContext context)
        {
            var books = context.Books.Where(a => a.Name.StartsWith("M")).Except(context.Books.Where(a => a.AuthorId % 2 == 0));

            foreach (var book in books) { Console.WriteLine(book.Name + " " + book.AuthorId); }
        }

        public static void Join(MyContext context)
        {
            var books = context.Books.Join(context.Authors,
                                            b => b.AuthorId,
                                            a => a.Id,
                                            (b, a) => new
                                            {
                                                b.Name,
                                                b.Genre,
                                                a.FirstName,
                                                a.LastName,
                                                a.CountryOfBirth
                                            }
                );
            foreach (var item in books) 
            {
                Console.WriteLine($"{item.Name} {item.Genre} {item.FirstName} {item.LastName}");
            }
        }

        public static void Distinct(MyContext context)
        {
            var booksCountry = context.Authors.Select(a => a.CountryOfBirth).Distinct().ToList();

            foreach (var country in booksCountry) { Console.WriteLine(country); }
        }

        public static void GroupByCount(MyContext context)
        {
            var lst = context.Books
                .GroupBy(o => o.AuthorId)
                .Select(s => new { Key = s.Key, Count = s.Count() });

            foreach (var item in lst) Console.WriteLine(item);
        }

        public static void MaxMinFunctions(MyContext context)
        {
            int maxId = context.Books.Max(o => o.AuthorId);
            int minId = context.Books.Min(o => o.AuthorId);
        }

        public static void EagerLoading(MyContext context)
        {
            var bookAuthor = context.Books.Include(b => b.Author).ToList();
            foreach (var item in bookAuthor)
            {
                Console.WriteLine($"{item.Name} by {item.Author.FirstName} {item.Author.LastName}");
            }
        }

        public static void ExplicitLoading(MyContext context) 
        {

            var author = context.Authors.FirstOrDefault();
            context.Books.Where(b => b.AuthorId == author.Id).Load();

            Console.WriteLine($"Author: {author.FirstName} {author.LastName}");
            Console.WriteLine("Books:");
            foreach(var item in author.Books) Console.WriteLine(item.Name);



        }

        public static void LazyLoading(MyContext context)
        {
            var book = context.Books.FirstOrDefault();
            Console.WriteLine(book.Author.FirstName);
        }

        public static void NoTracking(MyContext context)
        {
            var client = context.Clients.AsNoTracking().FirstOrDefault();


            Console.WriteLine(client.FirstName + " " + client.LastName);

            client.FirstName = "Oleg";
            client.LastName = "Sunny";

            context.SaveChanges();

            Console.WriteLine(context.Clients.FirstOrDefault().FirstName + " " + context.Clients.FirstOrDefault().LastName);
        }

        public static void InvokeSavedFunction(MyContext context)
        {
            var authors = context.GetAuthorById(2).FirstOrDefault();
            Console.WriteLine(authors.FirstName + " " + authors.LastName);
        }

        public static void InvokeSavedProcedure(MyContext context)
        {
            SqlParameter param = new()
            {
                ParameterName = "@id",
                SqlDbType = System.Data.SqlDbType.Int,
                Direction = System.Data.ParameterDirection.Output,
            };
            context.Database.ExecuteSqlRaw("GetMaxAuthorId @id OUT", param);
            Console.WriteLine(param.Value);
        }

        public static void SelectAuthors(MyContext context)
        {
            var count = context.Books
                .GroupBy(o => o.AuthorId)
                .Select(s => new { Key = s.Key, Count = s.Count() })
                .Where(a => a.Count > 3)
                .Count();

            Console.WriteLine(count);
        }
    }


}