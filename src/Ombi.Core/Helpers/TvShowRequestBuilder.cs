﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ombi.Api.TvMaze;
using Ombi.Api.TvMaze.Models;
using Ombi.Core.Models.Requests;
using Ombi.Core.Models.Search;
using Ombi.Helpers;
using Ombi.Store.Entities;
using Ombi.Store.Entities.Requests;
using Ombi.Store.Repository.Requests;

namespace Ombi.Core.Helpers
{
    public class TvShowRequestBuilder
    {

        public TvShowRequestBuilder(ITvMazeApi tvApi)
        {
            TvApi = tvApi;
        }
        
        private ITvMazeApi TvApi { get; }

        public ChildRequests ChildRequest { get; set; }
        public List<SeasonsViewModel> TvRequests { get; protected set; }
        public string PosterPath { get; protected set; }
        public DateTime FirstAir { get; protected set; }
        public TvRequests NewRequest { get; protected set; }
        protected TvMazeShow ShowInfo { get; set; }

        public async Task<TvShowRequestBuilder> GetShowInfo(int id)
        {
            ShowInfo = await TvApi.ShowLookupByTheTvDbId(id);

            DateTime.TryParse(ShowInfo.premiered, out var dt);

            FirstAir = dt;

            // For some reason the poster path is always http
            PosterPath = ShowInfo.image?.medium.Replace("http:", "https:");

            return this;
        }
        
        public TvShowRequestBuilder CreateChild(TvRequestViewModel model, string userId)
        {
            ChildRequest = new ChildRequests
            {
                Id = model.TvDbId,
                RequestType = RequestType.TvShow,
                RequestedDate = DateTime.UtcNow,
                Approved = false,
                RequestedUserId = userId,
                SeasonRequests = new List<SeasonRequests>(),
                Title = ShowInfo.name,
                SeriesType = ShowInfo.type.Equals("Animation", StringComparison.CurrentCultureIgnoreCase) ? SeriesType.Anime : SeriesType.Standard
            };

            return this;
        }

        public TvShowRequestBuilder CreateTvList(TvRequestViewModel tv)
        {
            TvRequests = new List<SeasonsViewModel>();
            // Only have the TV requests we actually requested and not everything
            foreach (var season in tv.Seasons)
            {
                if (season.Episodes.Any())
                {
                    TvRequests.Add(season);
                }
            }

            return this;
        }


        public async Task<TvShowRequestBuilder> BuildEpisodes(TvRequestViewModel tv)
        {
            if (tv.RequestAll)
            {
                var episodes = await TvApi.EpisodeLookup(ShowInfo.id);
                foreach (var ep in episodes)
                {
                    var season = ChildRequest.SeasonRequests.FirstOrDefault(x => x.SeasonNumber == ep.season);
                    if (season == null)
                    {
                        ChildRequest.SeasonRequests.Add(new SeasonRequests
                        {
                            Episodes = new List<EpisodeRequests>{
                                new EpisodeRequests
                                {
                                    EpisodeNumber = ep.number,
                                    AirDate = FormatDate(ep.airdate),
                                    Title = ep.name,
                                    Url = ep.url
                                }
                            },
                            SeasonNumber = ep.season,
                        });
                    }
                    else
                    {
                        season.Episodes.Add(new EpisodeRequests
                        {
                            EpisodeNumber = ep.number,
                            AirDate = FormatDate(ep.airdate),
                            Title = ep.name,
                            Url = ep.url
                        });
                    }
                }

            }
            else if (tv.LatestSeason)
            {
                var episodes = await TvApi.EpisodeLookup(ShowInfo.id);
                var latest = episodes.OrderByDescending(x => x.season).FirstOrDefault();
                var episodesRequests = new List<EpisodeRequests>();
                foreach (var ep in episodes)
                {
                    if (ep.season == latest.season)
                    {
                        episodesRequests.Add(new EpisodeRequests
                        {
                            EpisodeNumber = ep.number,
                            AirDate = FormatDate(ep.airdate),
                            Title = ep.name,
                            Url = ep.url
                        });
                    }
                }
                ChildRequest.SeasonRequests.Add(new SeasonRequests
                {
                    Episodes = episodesRequests,
                    SeasonNumber = latest.season,
                });
            }
            else if (tv.FirstSeason)
            {
                var episodes = await TvApi.EpisodeLookup(ShowInfo.id);
                var first = episodes.OrderBy(x => x.season).FirstOrDefault();
                var episodesRequests = new List<EpisodeRequests>();
                foreach (var ep in episodes)
                {
                    if (ep.season == first.season)
                    {
                        episodesRequests.Add(new EpisodeRequests
                        {
                            EpisodeNumber = ep.number,
                            AirDate = FormatDate(ep.airdate),
                            Title = ep.name,
                            Url = ep.url
                        });
                    }
                }
                ChildRequest.SeasonRequests.Add(new SeasonRequests
                {
                    Episodes = episodesRequests,
                    SeasonNumber = first.season,
                });
            }
            else
            {
                // It's a custom request
                var seasonRequests = new List<SeasonRequests>();
                var episodes = await TvApi.EpisodeLookup(ShowInfo.id);
                foreach (var ep in episodes)
                {
                    var existingSeasonRequest = seasonRequests.FirstOrDefault(x => x.SeasonNumber == ep.season);
                    if (existingSeasonRequest != null)
                    {
                        var requestedSeason = tv.Seasons.FirstOrDefault(x => x.SeasonNumber == ep.season);
                        var requestedEpisode = requestedSeason?.Episodes?.Any(x => x.EpisodeNumber == ep.number) ?? false;
                        if (requestedSeason != null && requestedEpisode)
                        {
                            // We already have this, let's just add the episodes to it
                            existingSeasonRequest.Episodes.Add(new EpisodeRequests
                            {
                                EpisodeNumber = ep.number,
                                AirDate = FormatDate(ep.airdate),
                                Title = ep.name,
                                Url = ep.url,
                            });
                        }
                    }
                    else
                    {
                        var newRequest = new SeasonRequests {SeasonNumber = ep.season};
                        var requestedSeason = tv.Seasons.FirstOrDefault(x => x.SeasonNumber == ep.season);
                        var requestedEpisode = requestedSeason?.Episodes?.Any(x => x.EpisodeNumber == ep.number) ?? false;
                        if (requestedSeason != null && requestedEpisode)
                        {
                            newRequest.Episodes.Add(new EpisodeRequests
                            {
                                EpisodeNumber = ep.number,
                                AirDate = FormatDate(ep.airdate),
                                Title = ep.name,
                                Url = ep.url,
                            });
                            seasonRequests.Add(newRequest);
                        }
                    }
                }

                foreach (var s in seasonRequests)
                {
                    ChildRequest.SeasonRequests.Add(s);
                }
            }
            return this;
        }
        
        
        public TvShowRequestBuilder CreateNewRequest(TvRequestViewModel tv)
        {
            NewRequest = new TvRequests
            {
                Overview = ShowInfo.summary.RemoveHtml(),
                PosterPath = PosterPath,
                Title = ShowInfo.name,
                ReleaseDate = FirstAir,
                Status = ShowInfo.status,
                ImdbId = ShowInfo.externals?.imdb ?? string.Empty,
                TvDbId = tv.TvDbId,
                ChildRequests = new List<ChildRequests>(),
                TotalSeasons = tv.Seasons.Count()
            };
            NewRequest.ChildRequests.Add(ChildRequest);

            return this;
        }

        private DateTime FormatDate(string date)
        {
            return string.IsNullOrEmpty(date) ? DateTime.MinValue : DateTime.Parse(date);
        }
    }
}