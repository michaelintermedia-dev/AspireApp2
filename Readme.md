dotnet ef dbcontext scaffold "Host=localhost;Port=5432;Database=recordings;Username=postgres;Password=postgres" Npgsql.EntityFrameworkCore.PostgreSQL --project WebAPI --output-dir DbData --context-dir DbData --context RecordingsDbContext --force

