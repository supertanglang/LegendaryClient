﻿namespace Sightstone.Patcher.Logic.UpdateRegion
{
    public sealed class Garena : Sightstone.Patcher.Logic.UpdateRegion.BaseUpdateRegion
    {
        public override string AirListing
        {
            get { return "http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/releases/releaselisting_NA"; }
        }

        public override string AirManifest
        {
            get { return "http://l3cdn.riotgames.com/releases/live/projects/lol_air_client/"; }
        }

        public override string GameListing
        {
            get { return "http://l3cdn.riotgames.com/releases/live/projects/lol_game_client/releases/releaselisting_NA"; }
        }

        public override string SolutionListing
        {
            get { return "http://l3cdn.riotgames.com/releases/live/solutions/lol_game_client_sln/releases/releaselisting_NA"; }
        }

        public override string SolutionManifest
        {
            get { return "http://l3cdn.riotgames.com/releases/live/solutions/lol_game_client_sln/"; }
        }

        public override string BaseLink
        {
            get { return "http://l3cdn.riotgames.com/releases/live"; }
        }
    }
}