﻿using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace JlzQualiTool
{
    [DataContract]
    public class Round : INotifyPropertyChanged
    {
        public static Round Zero = new Round();

        private static ILog Log = log4net.LogManager.GetLogger(typeof(Round));
        private RankingSnapshot ranking = RankingSnapshot.None;

        public bool HasStarted => !Matchups.All(m => !m.IsPlayed);
        public RoundInfo Info { get; } = new RoundInfo();
        public bool IsComplete => Matchups.All(m => m.IsPlayed);

        [DataMember(Order = 1)]
        public ObservableCollection<Matchup> Matchups { get; } = new ObservableCollection<Matchup>();

        [DataMember(Order = 0)]
        public int Number { get; private set; }

        public RankingSnapshot Ranking
        {
            get => ranking;
            set
            {
                ranking = value;
                RaiseOnRankingUpdatedEvent();
            }
        }

        internal Round PreviousRound { get; }

        private Func<IEnumerable<Matchup>, RankingSnapshot> RankingOrder { get; }

        private IMatchupStrategy Strategy { get; }

        public event EventHandler OnRankingUpdatedEvent = (o, e) =>
                        {
                            if (o != null)
                            {
                                ((Round) o).OnPropertyChanged("Ranking");
                            }
                        };

        public event PropertyChangedEventHandler? PropertyChanged;

        public Round(RoundInfo roundInfo, ViewModel viewModel, Func<IEnumerable<Matchup>, RankingSnapshot> rankingOrder)
        {
            this.Number = roundInfo.Number;
            this.PreviousRound = Number - 2 < 0 ? Zero : viewModel.Rounds[Number - 2];
            this.RankingOrder = rankingOrder;
            this.Info = roundInfo;

            this.Strategy = StrategyFactory.GetStrategy(this, viewModel);
            Strategy.CreateMatchups();
        }

        private Round()
        {
            Number = 0;
            Strategy = NoStrategy.Get;
            PreviousRound = Zero;
            RankingOrder = x => { return RankingSnapshot.None; };
        }

        public void RaiseOnRankingUpdatedEvent()
        {
            OnRankingUpdatedEvent?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateRanking(IEnumerable<Matchup> matchups)
        {
            Log.Info($"Updating ranking for round {Number}:");
            this.Ranking = RankingOrder.Invoke(matchups);
        }

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}