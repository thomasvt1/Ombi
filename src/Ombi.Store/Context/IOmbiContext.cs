﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Ombi.Store.Entities;
using Ombi.Store.Entities.Requests;

namespace Ombi.Store.Context
{
    public interface IOmbiContext : IDisposable
    {
        int SaveChanges();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
        DbSet<GlobalSettings> Settings { get; set; }
        DbSet<PlexServerContent> PlexServerContent { get; set; }
        DbSet<PlexEpisode> PlexEpisode { get; set; }
        DbSet<RadarrCache> RadarrCache { get; set; }
        DbSet<EmbyContent> EmbyContent { get; set; }
        DbSet<EmbyEpisode> EmbyEpisode { get; set; }
        DatabaseFacade Database { get; }
        EntityEntry<T> Entry<T>(T entry) where T : class;
        EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class;
        DbSet<TEntity> Set<TEntity>() where TEntity : class;
        DbSet<NotificationTemplates> NotificationTemplates { get; set; }
        DbSet<ApplicationConfiguration> ApplicationConfigurations { get; set; }
        void Seed();
        DbSet<Audit> Audit { get; set; }
        DbSet<MovieRequests> MovieRequests { get; set; }
        DbSet<TvRequests> TvRequests { get; set; }
        DbSet<ChildRequests> ChildRequests { get; set; }
        DbSet<Issues> Issues { get; set; }
        DbSet<IssueCategory> IssueCategories { get; set; }
        DbSet<Tokens> Tokens { get; set; }
        DbSet<SonarrCache> SonarrCache { get; set; }
        DbSet<SonarrEpisodeCache> SonarrEpisodeCache { get; set; }
        EntityEntry Update(object entity);
        EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;
        DbSet<CouchPotatoCache> CouchPotatoCache { get; set; }
        DbSet<SickRageCache> SickRageCache { get; set; }
        DbSet<SickRageEpisodeCache> SickRageEpisodeCache { get; set; }
        DbSet<RequestLog> RequestLogs { get; set; }
        DbSet<RecentlyAddedLog> RecentlyAddedLogs { get; set; }
    }
}