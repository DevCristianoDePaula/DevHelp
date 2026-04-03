using DevHelp.Models.Identity;
using DevHelp.Models.Admin;
using DevHelp.Models.Tickets;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DevHelp.Data
{
    // Contexto de banco da aplicação, especializado para o usuário customizado do sistema.
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        // Tabela de categorias administrativas do sistema.
        public DbSet<Category> Categories => Set<Category>();

        // Tabela principal de chamados abertos pelos alunos.
        public DbSet<Ticket> Tickets => Set<Ticket>();

        // Tabela de anexos e links externos vinculados aos chamados.
        public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();

        // Tabela de comentários e interações dos chamados.
        public DbSet<TicketComment> TicketComments => Set<TicketComment>();

        // Tabela de anúncios de chamada exibidos no painel público da TV.
        public DbSet<TicketCallAnnouncement> TicketCallAnnouncements => Set<TicketCallAnnouncement>();

        // Tabela de jobs de exportação assíncrona de histórico de atendimentos.
        public DbSet<AttendanceReportExportJob> AttendanceReportExportJobs => Set<AttendanceReportExportJob>();

        // Configurações adicionais de relacionamento e restrições do modelo.
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Ticket>(entity =>
            {
                entity.HasOne(t => t.Student)
                    .WithMany()
                    .HasForeignKey(t => t.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.Category)
                    .WithMany()
                    .HasForeignKey(t => t.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.AssignedProfessor)
                    .WithMany()
                    .HasForeignKey(t => t.AssignedProfessorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.PreferredProfessor)
                    .WithMany()
                    .HasForeignKey(t => t.PreferredProfessorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<AttendanceReportExportJob>(entity =>
            {
                entity.HasOne(j => j.Requester)
                    .WithMany()
                    .HasForeignKey(j => j.RequesterId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<TicketAttachment>(entity =>
            {
                entity.HasOne(a => a.Ticket)
                    .WithMany(t => t.Attachments)
                    .HasForeignKey(a => a.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<TicketComment>(entity =>
            {
                entity.HasOne(c => c.Ticket)
                    .WithMany(t => t.Comments)
                    .HasForeignKey(c => c.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Author)
                    .WithMany()
                    .HasForeignKey(c => c.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
