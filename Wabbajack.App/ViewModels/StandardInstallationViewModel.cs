using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wabbajack.App.Messages;
using Wabbajack.DTOs;
using Wabbajack.Installer;

namespace Wabbajack.App.ViewModels
{
    public class StandardInstallationViewModel : ViewModelBase, IReceiver<StartInstallation>
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
        
        public void Receive(StartInstallation msg)
        {
            MessageBus.Instance.Send(new NavigateTo(GetType()));
            
            _scope = _provider.CreateScope();
            _config = _provider.GetService<InstallerConfiguration>()!;
            _config.Downloads = msg.Download;
            _config.ModList = msg.ModList;
            _config.Install = msg.Install;
            _config.ModlistArchive = msg.ModListPath;
            _config.Game = msg.ModList.GameType;

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