namespace Cursos.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class UnificarContextos : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "HasSignatureFooter", c => c.Boolean(nullable: false, defaultValue: false));
            AddColumn("dbo.AspNetUsers", "SignatureFooter", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "SignatureFooter");
            DropColumn("dbo.AspNetUsers", "HasSignatureFooter");
        }
    }
}

