using System;
using System.Collections.Generic;
using System.Linq;
using GameFinder.StoreHandlers.Steam;
using Microsoft.Extensions.Logging;
using Wabbajack.DTOs;
using Wabbajack.Paths;

namespace Wabbajack.Installer
{
    public class GameLocator : IGameLocator
    {
        private readonly Dictionary<Game, AbsolutePath> _locationCache;
        private readonly ILogger<GameLocator> _logger;
        private readonly SteamHandler _steam;

        public GameLocator(ILogger<GameLocator> logger)
        {
            _logger = logger;
            _steam = new SteamHandler(logger);
            _locationCache = new Dictionary<Game, AbsolutePath>();
        }

        public AbsolutePath GameLocation(Game game)
        {
            if (TryFindLocation(game, out var path))
                return path;
            throw new Exception($"Can't find game {game}");
        }

        public bool IsInstalled(Game game)
        {
            return TryFindLocation(game, out _);
        }

        public bool TryFindLocation(Game game, out AbsolutePath path)
        {
            lock (_locationCache)
            {
                if (_locationCache.TryGetValue(game, out path))
                    return true;

                if (TryFindLocationInner(game, out path))
                {
                    _locationCache.Add(game, path);
                    return true;
                }
            }

            return false;
        }

        private bool TryFindLocationInner(Game game, out AbsolutePath path)
        {
            var metaData = game.MetaData();
            if (_steam.FindAllGames())
                foreach (var steamGame in _steam.Games)
                    if (metaData.SteamIDs.Contains(steamGame.ID))
                    {
                        path = steamGame!.Path.ToAbsolutePath();
                        return true;
                    }

            path = default;
            return false;
        }
    }
}