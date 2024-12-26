using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace testCitybreak.Models;

public partial class CitybreakContext : DbContext
{
    public CitybreakContext(DbContextOptions<CitybreakContext> options)
        : base(options)
    {
    }

    public virtual DbSet<memberTable> memberTable { get; set; }

    public virtual DbSet<orderTable> orderTable { get; set; }

    public virtual DbSet<order_details> order_details { get; set; }

    public virtual DbSet<productTable> productTable { get; set; }

    public virtual DbSet<product_classification> product_classification { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<memberTable>(entity =>
        {
            entity.HasKey(e => e.userID).HasName("PK__memberTa__3214EC2741F853C5");

            entity.ToTable(tb => tb.HasTrigger("generate_user_id"));

            entity.HasIndex(e => e.email, "UQ__memberTa__AB6E61641BF89B4E").IsUnique();

            entity.HasIndex(e => e.phone, "UQ__memberTa__B43B145F223FC5AA").IsUnique();

            entity.Property(e => e.createdDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.email).HasMaxLength(50);
            entity.Property(e => e.name).HasMaxLength(10);
            entity.Property(e => e.phone)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.webMemberID)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<orderTable>(entity =>
        {
            entity.HasKey(e => e.orderID).HasName("PK__orderTab__0809337DD9215CF7");

            entity.HasIndex(e => e.merchantTradeNo, "UQ__orderTab__1BDB5A826E405A43").IsUnique();

            entity.Property(e => e.latestUpdatedTime)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.merchantTradeNo).HasMaxLength(50);
            entity.Property(e => e.orderStatus)
                .HasMaxLength(20)
                .HasDefaultValue("未付款");
            entity.Property(e => e.orderTime)
                .HasPrecision(0)
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.totalPrice).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.user).WithMany(p => p.orderTable)
                .HasForeignKey(d => d.userID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_userID");
        });

        modelBuilder.Entity<order_details>(entity =>
        {
            entity.HasKey(e => e.detailID).HasName("PK__order_de__83077839B564C002");

            entity.HasOne(d => d.order).WithMany(p => p.order_details)
                .HasForeignKey(d => d.orderID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_orderID");

            entity.HasOne(d => d.product).WithMany(p => p.order_details)
                .HasForeignKey(d => d.productID)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_productID");
        });

        modelBuilder.Entity<productTable>(entity =>
        {
            entity.HasKey(e => e.productID).HasName("PK__productT__2D10D14A64FFC7E3");

            entity.Property(e => e.productName).HasMaxLength(50);
            entity.Property(e => e.unitStock).HasDefaultValue((byte)100);

            entity.HasOne(d => d.classification).WithMany(p => p.productTable)
                .HasForeignKey(d => d.classificationID)
                .HasConstraintName("FK_classificationID");
        });

        modelBuilder.Entity<product_classification>(entity =>
        {
            entity.HasKey(e => e.classificationID).HasName("PK__product___93F59CB67C46909D");

            entity.HasIndex(e => e.classification, "UQ_classification").IsUnique();

            entity.Property(e => e.classificationID).ValueGeneratedOnAdd();
            entity.Property(e => e.classification).HasMaxLength(20);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
