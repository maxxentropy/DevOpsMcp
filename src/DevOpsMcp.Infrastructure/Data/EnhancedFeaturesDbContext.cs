using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevOpsMcp.Domain.Entities;
using DevOpsMcp.Domain.Entities.Enhanced;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace DevOpsMcp.Infrastructure.Data;

public class EnhancedFeaturesDbContext : DbContext
{
    public EnhancedFeaturesDbContext(DbContextOptions<EnhancedFeaturesDbContext> options) 
        : base(options) { }
    
    // DbSets for enhanced features
    public DbSet<EnhancedProject> Projects { get; set; }
    public DbSet<DevOpsTask> Tasks { get; set; }
    public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; }
    public DbSet<KnowledgeSource> KnowledgeSources { get; set; }
    public DbSet<DocumentVersion> DocumentVersions { get; set; }
    public DbSet<ProjectSource> ProjectSources { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure EnhancedProject
        modelBuilder.Entity<EnhancedProject>(entity =>
        {
            entity.ToTable("archon_projects");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasDefaultValue("");
            entity.Property(e => e.GithubRepo).HasColumnName("github_repo");
            entity.Property(e => e.Pinned).HasColumnName("pinned");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            
            // Configure JSONB columns
            entity.Property(e => e.Docs)
                .HasColumnName("docs")
                .HasColumnType("jsonb")
                .HasConversion(new JsonDocumentValueConverter());
                
            entity.Property(e => e.Features)
                .HasColumnName("features")
                .HasColumnType("jsonb")
                .HasConversion(new JsonDocumentValueConverter());
                
            entity.Property(e => e.Data)
                .HasColumnName("data")
                .HasColumnType("jsonb")
                .HasConversion(new JsonDocumentValueConverter());
        });
        
        // Configure DevOpsTask
        modelBuilder.Entity<DevOpsTask>(entity =>
        {
            entity.ToTable("archon_tasks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ParentTaskId).HasColumnName("parent_task_id");
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasDefaultValue("");
            entity.Property(e => e.Assignee).HasColumnName("assignee").HasDefaultValue("User");
            entity.Property(e => e.TaskOrder).HasColumnName("task_order");
            entity.Property(e => e.Feature).HasColumnName("feature");
            entity.Property(e => e.Archived).HasColumnName("archived");
            entity.Property(e => e.ArchivedAt).HasColumnName("archived_at");
            entity.Property(e => e.ArchivedBy).HasColumnName("archived_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            
            // Configure enum conversion
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion(
                    v => v.ToString().ToLowerInvariant(),
                    v => Enum.Parse<DevOpsTaskStatus>(v, true)
                );
            
            // Configure JSONB columns
            entity.Property(e => e.Sources)
                .HasColumnName("sources")
                .HasColumnType("jsonb")
                .HasConversion(new JsonDocumentValueConverter());
                
            entity.Property(e => e.CodeExamples)
                .HasColumnName("code_examples")
                .HasColumnType("jsonb")
                .HasConversion(new JsonDocumentValueConverter());
            
            // Configure relationships
            entity.HasOne(e => e.Project)
                .WithMany(p => p.Tasks)
                .HasForeignKey(e => e.ProjectId);
                
            entity.HasOne(e => e.ParentTask)
                .WithMany(p => p.SubTasks)
                .HasForeignKey(e => e.ParentTaskId);
        });
        
        // Configure KnowledgeDocument
        modelBuilder.Entity<KnowledgeDocument>(entity =>
        {
            entity.ToTable("archon_crawled_pages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Url).HasColumnName("url").IsRequired();
            entity.Property(e => e.ChunkNumber).HasColumnName("chunk_number");
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.SourceId).HasColumnName("source_id").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            
            // Configure JSONB column
            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb")
                .HasConversion(new JsonDocumentValueConverter());
            
            // Configure vector column with conversion
            entity.Property(e => e.Embedding)
                .HasColumnName("embedding")
                .HasColumnType("vector(1536)")
                .HasConversion(
                    v => v == null ? null : new Vector(v as float[] ?? v.ToArray()),
                    v => v == null ? null : (IReadOnlyList<float>)v.ToArray());
                
            entity.HasIndex(e => new { e.Url, e.ChunkNumber }).IsUnique();
        });
        
        // Configure KnowledgeSource
        modelBuilder.Entity<KnowledgeSource>(entity =>
        {
            entity.ToTable("archon_sources");
            entity.HasKey(e => e.SourceId);
            entity.Property(e => e.SourceId).HasColumnName("source_id");
            entity.Property(e => e.Summary).HasColumnName("summary");
            entity.Property(e => e.TotalWordCount).HasColumnName("total_word_count");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            
            // Configure JSONB column
            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb")
                .HasConversion(new JsonDocumentValueConverter());
        });
        
        // Configure DocumentVersion
        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.ToTable("archon_document_versions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.TaskId).HasColumnName("task_id");
            entity.Property(e => e.FieldName).HasColumnName("field_name").IsRequired();
            entity.Property(e => e.VersionNumber).HasColumnName("version_number");
            entity.Property(e => e.ChangeSummary).HasColumnName("change_summary");
            entity.Property(e => e.ChangeType).HasColumnName("change_type").HasDefaultValue("update");
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasDefaultValue("system");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            
            // Configure JSONB column
            entity.Property(e => e.Content)
                .HasColumnName("content")
                .HasColumnType("jsonb")
                .HasConversion(new JsonDocumentValueConverter());
                
            entity.HasOne(e => e.Project)
                .WithMany(p => p.DocumentVersions)
                .HasForeignKey(e => e.ProjectId);
        });
        
        // Configure ProjectSource
        modelBuilder.Entity<ProjectSource>(entity =>
        {
            entity.ToTable("archon_project_sources");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.SourceId).HasColumnName("source_id").IsRequired();
            entity.Property(e => e.LinkedAt).HasColumnName("linked_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasDefaultValue("system");
            entity.Property(e => e.Notes).HasColumnName("notes");
            
            entity.HasIndex(e => new { e.ProjectId, e.SourceId }).IsUnique();
            
            entity.HasOne(e => e.Project)
                .WithMany(p => p.ProjectSources)
                .HasForeignKey(e => e.ProjectId);
                
            entity.HasOne(e => e.Source)
                .WithMany(s => s.ProjectSources)
                .HasForeignKey(e => e.SourceId);
        });
        
        // Configure KnowledgeDocument -> KnowledgeSource relationship
        modelBuilder.Entity<KnowledgeDocument>()
            .HasOne(e => e.Source)
            .WithMany(s => s.Documents)
            .HasForeignKey(e => e.SourceId);
    }
}

// Custom value converter for JsonDocument
public class JsonDocumentValueConverter : ValueConverter<JsonDocument, string>
{
    public JsonDocumentValueConverter() : base(
        v => v.RootElement.GetRawText(),
        v => string.IsNullOrEmpty(v) ? JsonDocument.Parse("{}", default(JsonDocumentOptions)) : JsonDocument.Parse(v, default(JsonDocumentOptions)))
    {
    }
}