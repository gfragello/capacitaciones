public class ApplicationUser : IdentityUser
{
    [AllowHtml]
    public string SignatureFooter { get; set; }

    public bool HasSignatureFooter { get; set; }
}

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext()
        : base("DefaultConnection", throwIfV1Schema: false)
    {
    }

    public static ApplicationDbContext Create()
    {
        return new ApplicationDbContext();
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ensure the new properties are included in the model
        modelBuilder.Entity<ApplicationUser>().Property(u => u.SignatureFooter).IsOptional();
        modelBuilder.Entity<ApplicationUser>().Property(u => u.HasSignatureFooter).IsOptional();
    }
}
