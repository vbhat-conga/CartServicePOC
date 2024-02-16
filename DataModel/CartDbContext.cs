using Microsoft.EntityFrameworkCore;

namespace CartServicePOC.DataModel
{
    public class CartDbContext : DbContext
    {
        public CartDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<CartData> Carts { get; set; }

        public DbSet<CartItemData> CartItemDatas { get; set; }

        public DbSet<CartStatusType> CartStatuses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CartItemData>()
                .ToTable("CartItem")
                .HasKey(c => c.CartItemId);


            modelBuilder.Entity<CartItemData>()
                .Property(x => x.CartItemId)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedNever();

            modelBuilder.Entity<CartItemData>()
                .Property(x => x.IsPrimaryLine)
                .HasDefaultValue(true);

            modelBuilder.Entity<CartItemData>()
                .Property(x => x.LineType)
                .HasConversion<int>();

            modelBuilder.Entity<CartItemData>()
                .HasOne(c=>c.cart)
                .WithMany(c=>c.CartItems)
                .HasForeignKey(c=>c.CartId)
                .IsRequired();

            modelBuilder.Entity<CartItemData>()
                .Property(x => x.CartId)
                .HasColumnType("uniqueidentifier");

            modelBuilder.Entity<CartItemData>()
               .HasIndex(c => c.CartId)
               .HasDatabaseName("idx_CartId");

            modelBuilder.Entity<CartData>()
                .ToTable("Cart")
                .HasKey(c => c.CartId);

            modelBuilder.Entity<CartData>()
                .Property(x => x.CartId)
                .HasColumnType("uniqueidentifier")
                .ValueGeneratedNever();

            modelBuilder.Entity<CartData>()
                .Property(x => x.Name)
                .HasColumnType("varchar")
                .HasMaxLength(50);

            modelBuilder.Entity<CartData>()
                .Property(c => c.Status)
                .HasConversion<int>();

            modelBuilder.Entity<CartStatusType>()
                .ToTable("CartStatus")
                .HasKey(c => c.Id);

            modelBuilder.Entity<CartStatusType>()
                .Property(x => x.Id)
                .ValueGeneratedNever();

            modelBuilder.Entity<CartStatusType>()
                .HasData(
                new CartStatusType
                {
                    Id = 0,
                    Status = "Unknown"
                },
                new CartStatusType
                {
                    Id = 1,
                    Status = "Created"
                },
                new CartStatusType
                {
                    Id = 2,
                    Status = "Configured"
                },
                new CartStatusType
                {
                    Id = 3,
                    Status = "Priced"
                });
           base.OnModelCreating(modelBuilder);

        }
    }
}
