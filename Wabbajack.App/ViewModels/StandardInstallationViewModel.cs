using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wabbajack.App.Interfaces;
using Wabbajack.DTOs;
using Wabbajack.Installer;

namespace Wabbajack.App.ViewModels
{
    public class StandardInstallationViewModel : ViewModelBase, INavigationParameter<InstallerConfiguration>
    {
        private readonly IServiceProvider _provider;
        private readonly GameLocator _locator;
        private IServiceScope _scope;
        private InstallerConfiguration _config;
        private StandardInstaller _installer;
        private readonly ILogger<StandardInstallationViewModel> _logger;

        public StandardInstallationViewModel(ILogger<StandardInstallationViewModel> logger, IServiceProvider provider, GameLocator locator)
        {
            _provider = provider;
            _locator = locator;
            _logger = logger;
        }
        public async Task NavigatedTo(InstallerConfiguration param)
        {
            _scope = _provider.CreateScope();
            _config = _provider.GetService<InstallerConfiguration>()!;
            _config.Downloads = param.Downloads;
            _config.ModList = param.ModList;
            _config.Install = param.Install;
            _config.ModlistArchive = param.ModlistArchive;
            _config.Game = param.ModList.GameType;

            if (_config.GameFolder == default)
            {
                if (!_locator.TryFindLocation(_config.Game, out var found))
                {
                    _logger.LogCritical("Game {game} is not installed on this system", _config.Game.MetaData().HumanFriendlyGameName);
                    throw new Exception("Game not found");
                }
                _config.GameFolder = found;
            }

            _installer = _provider.GetService<StandardInstaller>()!;
        }
    }
}