﻿// <auto-generated />
using System;
using FitWifFrens.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FitWifFrens.Data.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("FitWifFrens.Data.Commitment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ContractAddress")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Days")
                        .HasColumnType("integer");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Image")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateOnly>("StartDate")
                        .HasColumnType("date");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Commitments");
                });

            modelBuilder.Entity("FitWifFrens.Data.CommitmentPeriod", b =>
                {
                    b.Property<Guid>("CommitmentId")
                        .HasColumnType("uuid");

                    b.Property<DateOnly>("StartDate")
                        .HasColumnType("date");

                    b.Property<DateOnly>("EndDate")
                        .HasColumnType("date");

                    b.HasKey("CommitmentId", "StartDate", "EndDate");

                    b.ToTable("CommitmentPeriods");
                });

            modelBuilder.Entity("FitWifFrens.Data.CommitmentPeriodUser", b =>
                {
                    b.Property<Guid>("CommitmentId")
                        .HasColumnType("uuid");

                    b.Property<DateOnly>("StartDate")
                        .HasColumnType("date");

                    b.Property<DateOnly>("EndDate")
                        .HasColumnType("date");

                    b.Property<string>("UserId")
                        .HasColumnType("text");

                    b.Property<decimal>("Reward")
                        .HasColumnType("numeric");

                    b.Property<decimal>("Stake")
                        .HasColumnType("numeric");

                    b.HasKey("CommitmentId", "StartDate", "EndDate", "UserId");

                    b.HasIndex("UserId");

                    b.ToTable("CommitmentPeriodUsers");
                });

            modelBuilder.Entity("FitWifFrens.Data.CommitmentUser", b =>
                {
                    b.Property<Guid>("CommitmentId")
                        .HasColumnType("uuid");

                    b.Property<string>("UserId")
                        .HasColumnType("text");

                    b.Property<decimal>("Stake")
                        .HasColumnType("numeric");

                    b.HasKey("CommitmentId", "UserId");

                    b.HasIndex("UserId");

                    b.ToTable("CommitmentUsers");
                });

            modelBuilder.Entity("FitWifFrens.Data.Deposit", b =>
                {
                    b.Property<string>("Transaction")
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)");

                    b.Property<decimal>("Amount")
                        .HasColumnType("numeric");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Transaction");

                    b.HasIndex("UserId");

                    b.ToTable("Deposits");
                });

            modelBuilder.Entity("FitWifFrens.Data.Goal", b =>
                {
                    b.Property<Guid>("CommitmentId")
                        .HasColumnType("uuid");

                    b.Property<string>("ProviderName")
                        .HasColumnType("character varying(256)");

                    b.Property<string>("MetricName")
                        .HasColumnType("character varying(256)");

                    b.Property<string>("MetricType")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("Rule")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<double>("Value")
                        .HasColumnType("double precision");

                    b.HasKey("CommitmentId", "ProviderName", "MetricName", "MetricType");

                    b.HasIndex("ProviderName", "MetricName", "MetricType");

                    b.ToTable("Goals");
                });

            modelBuilder.Entity("FitWifFrens.Data.Metric", b =>
                {
                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Name");

                    b.ToTable("Metrics");

                    b.HasData(
                        new
                        {
                            Name = "Exercise"
                        },
                        new
                        {
                            Name = "Running"
                        },
                        new
                        {
                            Name = "Weight"
                        });
                });

            modelBuilder.Entity("FitWifFrens.Data.MetricValue", b =>
                {
                    b.Property<string>("MetricName")
                        .HasColumnType("character varying(256)");

                    b.Property<string>("Type")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("MetricName", "Type");

                    b.ToTable("MetricValues");

                    b.HasData(
                        new
                        {
                            MetricName = "Exercise",
                            Type = "Count"
                        },
                        new
                        {
                            MetricName = "Exercise",
                            Type = "Minutes"
                        },
                        new
                        {
                            MetricName = "Running",
                            Type = "Count"
                        },
                        new
                        {
                            MetricName = "Running",
                            Type = "Minutes"
                        },
                        new
                        {
                            MetricName = "Weight",
                            Type = "Value"
                        });
                });

            modelBuilder.Entity("FitWifFrens.Data.Provider", b =>
                {
                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Name");

                    b.ToTable("Providers");

                    b.HasData(
                        new
                        {
                            Name = "Strava"
                        },
                        new
                        {
                            Name = "Withings"
                        });
                });

            modelBuilder.Entity("FitWifFrens.Data.ProviderMetricValue", b =>
                {
                    b.Property<string>("ProviderName")
                        .HasColumnType("character varying(256)");

                    b.Property<string>("MetricName")
                        .HasColumnType("character varying(256)");

                    b.Property<string>("MetricType")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("ProviderName", "MetricName", "MetricType");

                    b.HasIndex("MetricName", "MetricType");

                    b.ToTable("ProviderMetricValues");

                    b.HasData(
                        new
                        {
                            ProviderName = "Strava",
                            MetricName = "Exercise",
                            MetricType = "Count"
                        },
                        new
                        {
                            ProviderName = "Strava",
                            MetricName = "Exercise",
                            MetricType = "Minutes"
                        },
                        new
                        {
                            ProviderName = "Strava",
                            MetricName = "Running",
                            MetricType = "Count"
                        },
                        new
                        {
                            ProviderName = "Strava",
                            MetricName = "Running",
                            MetricType = "Minutes"
                        },
                        new
                        {
                            ProviderName = "Withings",
                            MetricName = "Weight",
                            MetricType = "Value"
                        });
                });

            modelBuilder.Entity("FitWifFrens.Data.Role", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("FitWifFrens.Data.RoleClaim", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("FitWifFrens.Data.User", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("integer");

                    b.Property<decimal>("Balance")
                        .HasColumnType("numeric");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("boolean");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("text");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("boolean");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("AspNetUsers", (string)null);
                });

            modelBuilder.Entity("FitWifFrens.Data.UserClaim", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("FitWifFrens.Data.UserLogin", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("character varying(256)");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("text");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("text");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("FitWifFrens.Data.UserRole", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("text");

                    b.Property<string>("RoleId")
                        .HasColumnType("text");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("FitWifFrens.Data.UserToken", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("text");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("FitWifFrens.Data.CommitmentPeriod", b =>
                {
                    b.HasOne("FitWifFrens.Data.Commitment", "Commitment")
                        .WithMany("Periods")
                        .HasForeignKey("CommitmentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Commitment");
                });

            modelBuilder.Entity("FitWifFrens.Data.CommitmentPeriodUser", b =>
                {
                    b.HasOne("FitWifFrens.Data.User", "User")
                        .WithMany("CommitmentPeriods")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FitWifFrens.Data.CommitmentPeriod", "Commitment")
                        .WithMany("Users")
                        .HasForeignKey("CommitmentId", "StartDate", "EndDate")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Commitment");

                    b.Navigation("User");
                });

            modelBuilder.Entity("FitWifFrens.Data.CommitmentUser", b =>
                {
                    b.HasOne("FitWifFrens.Data.Commitment", "Commitment")
                        .WithMany("Users")
                        .HasForeignKey("CommitmentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FitWifFrens.Data.User", "User")
                        .WithMany("Commitments")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Commitment");

                    b.Navigation("User");
                });

            modelBuilder.Entity("FitWifFrens.Data.Deposit", b =>
                {
                    b.HasOne("FitWifFrens.Data.User", "User")
                        .WithMany("Deposits")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FitWifFrens.Data.Goal", b =>
                {
                    b.HasOne("FitWifFrens.Data.Commitment", "Commitment")
                        .WithMany("Goals")
                        .HasForeignKey("CommitmentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FitWifFrens.Data.ProviderMetricValue", "Metric")
                        .WithMany("Goals")
                        .HasForeignKey("ProviderName", "MetricName", "MetricType")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Commitment");

                    b.Navigation("Metric");
                });

            modelBuilder.Entity("FitWifFrens.Data.MetricValue", b =>
                {
                    b.HasOne("FitWifFrens.Data.Metric", "Metric")
                        .WithMany("Values")
                        .HasForeignKey("MetricName")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Metric");
                });

            modelBuilder.Entity("FitWifFrens.Data.ProviderMetricValue", b =>
                {
                    b.HasOne("FitWifFrens.Data.Provider", "Provider")
                        .WithMany("Metrics")
                        .HasForeignKey("ProviderName")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FitWifFrens.Data.MetricValue", "MetricValue")
                        .WithMany("Providers")
                        .HasForeignKey("MetricName", "MetricType")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("MetricValue");

                    b.Navigation("Provider");
                });

            modelBuilder.Entity("FitWifFrens.Data.RoleClaim", b =>
                {
                    b.HasOne("FitWifFrens.Data.Role", "Role")
                        .WithMany("RoleClaims")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Role");
                });

            modelBuilder.Entity("FitWifFrens.Data.UserClaim", b =>
                {
                    b.HasOne("FitWifFrens.Data.User", "User")
                        .WithMany("Claims")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FitWifFrens.Data.UserLogin", b =>
                {
                    b.HasOne("FitWifFrens.Data.Provider", "Provider")
                        .WithMany("Logins")
                        .HasForeignKey("LoginProvider")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FitWifFrens.Data.User", "User")
                        .WithMany("Logins")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Provider");

                    b.Navigation("User");
                });

            modelBuilder.Entity("FitWifFrens.Data.UserRole", b =>
                {
                    b.HasOne("FitWifFrens.Data.Role", "Role")
                        .WithMany("UserRoles")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FitWifFrens.Data.User", "User")
                        .WithMany("UserRoles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Role");

                    b.Navigation("User");
                });

            modelBuilder.Entity("FitWifFrens.Data.UserToken", b =>
                {
                    b.HasOne("FitWifFrens.Data.User", "User")
                        .WithMany("Tokens")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("FitWifFrens.Data.Commitment", b =>
                {
                    b.Navigation("Goals");

                    b.Navigation("Periods");

                    b.Navigation("Users");
                });

            modelBuilder.Entity("FitWifFrens.Data.CommitmentPeriod", b =>
                {
                    b.Navigation("Users");
                });

            modelBuilder.Entity("FitWifFrens.Data.Metric", b =>
                {
                    b.Navigation("Values");
                });

            modelBuilder.Entity("FitWifFrens.Data.MetricValue", b =>
                {
                    b.Navigation("Providers");
                });

            modelBuilder.Entity("FitWifFrens.Data.Provider", b =>
                {
                    b.Navigation("Logins");

                    b.Navigation("Metrics");
                });

            modelBuilder.Entity("FitWifFrens.Data.ProviderMetricValue", b =>
                {
                    b.Navigation("Goals");
                });

            modelBuilder.Entity("FitWifFrens.Data.Role", b =>
                {
                    b.Navigation("RoleClaims");

                    b.Navigation("UserRoles");
                });

            modelBuilder.Entity("FitWifFrens.Data.User", b =>
                {
                    b.Navigation("Claims");

                    b.Navigation("CommitmentPeriods");

                    b.Navigation("Commitments");

                    b.Navigation("Deposits");

                    b.Navigation("Logins");

                    b.Navigation("Tokens");

                    b.Navigation("UserRoles");
                });
#pragma warning restore 612, 618
        }
    }
}
