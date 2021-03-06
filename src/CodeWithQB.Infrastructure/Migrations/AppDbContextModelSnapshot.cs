﻿// <auto-generated />
using System;
using CodeWithQB.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CodeWithQB.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.0-rtm-35687")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("CodeWithQB.Core.Models.ContactRequest", b =>
                {
                    b.Property<Guid>("ContactRequestId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Body");

                    b.Property<string>("EmailAddress");

                    b.Property<string>("Name");

                    b.HasKey("ContactRequestId");

                    b.ToTable("ContactRequests");
                });

            modelBuilder.Entity("CodeWithQB.Core.Models.Customer", b =>
                {
                    b.Property<Guid>("CustomerId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.HasKey("CustomerId");

                    b.ToTable("Customers");
                });

            modelBuilder.Entity("CodeWithQB.Core.Models.DigitalAsset", b =>
                {
                    b.Property<Guid>("DigitalAssetId")
                        .ValueGeneratedOnAdd();

                    b.Property<byte[]>("Bytes");

                    b.Property<string>("ContentType");

                    b.Property<string>("Name");

                    b.HasKey("DigitalAssetId");

                    b.ToTable("DigitalAssets");
                });

            modelBuilder.Entity("CodeWithQB.Core.Models.Location", b =>
                {
                    b.Property<Guid>("LocationId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name");

                    b.HasKey("LocationId");

                    b.ToTable("Locations");
                });

            modelBuilder.Entity("CodeWithQB.Core.Models.Mentor", b =>
                {
                    b.Property<Guid>("MentorId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("FullName");

                    b.Property<string>("ImageUrl");

                    b.Property<string>("Title");

                    b.HasKey("MentorId");

                    b.ToTable("Mentors");
                });

            modelBuilder.Entity("CodeWithQB.Core.Models.MentorTag", b =>
                {
                    b.Property<Guid>("MentorId");

                    b.Property<Guid>("TagId");

                    b.HasKey("MentorId", "TagId");

                    b.HasIndex("TagId");

                    b.ToTable("MentorTag");
                });

            modelBuilder.Entity("CodeWithQB.Core.Models.Product", b =>
                {
                    b.Property<Guid>("ProductId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Description");

                    b.Property<string>("ImageUrl");

                    b.Property<string>("Name");

                    b.Property<float>("Price");

                    b.HasKey("ProductId");

                    b.ToTable("Products");
                });

            modelBuilder.Entity("CodeWithQB.Core.Models.Tag", b =>
                {
                    b.Property<Guid>("TagId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Description");

                    b.Property<string>("Name");

                    b.HasKey("TagId");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("CodeWithQB.Core.Models.Talk", b =>
                {
                    b.Property<Guid>("TalkId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Abstract");

                    b.Property<string>("Description");

                    b.HasKey("TalkId");

                    b.ToTable("Talks");
                });

            modelBuilder.Entity("CodeWithQB.Core.Models.User", b =>
                {
                    b.Property<Guid>("UserId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Password");

                    b.Property<byte[]>("Salt");

                    b.Property<string>("Username");

                    b.HasKey("UserId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("CodeWithQB.Core.Models.Location", b =>
                {
                    b.OwnsOne("CodeWithQB.Core.Models.Address", "Address", b1 =>
                        {
                            b1.Property<Guid>("LocationId");

                            b1.Property<string>("City");

                            b1.Property<string>("PostalCode");

                            b1.Property<string>("Province");

                            b1.Property<string>("Street");

                            b1.HasKey("LocationId");

                            b1.ToTable("Locations");

                            b1.HasOne("CodeWithQB.Core.Models.Location")
                                .WithOne("Address")
                                .HasForeignKey("CodeWithQB.Core.Models.Address", "LocationId")
                                .OnDelete(DeleteBehavior.Cascade);
                        });
                });

            modelBuilder.Entity("CodeWithQB.Core.Models.MentorTag", b =>
                {
                    b.HasOne("CodeWithQB.Core.Models.Mentor", "Mentor")
                        .WithMany()
                        .HasForeignKey("MentorId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("CodeWithQB.Core.Models.Tag", "Tag")
                        .WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
