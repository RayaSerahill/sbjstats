using System;
using System.Collections.Generic;

namespace sbjStats;

public class StatsRecording
{
    public long Time;
    public int BetsCollected;
    public int Payouts;
    public List<string> Players = [];
    public bool Saved = false;
    public string ArchiveID = Guid.Empty.ToString();
    public List<HandStat> Hands = [];
}

public class HandStat
{
    public string PlayerName = string.Empty;
    public List<Card> Cards = [];
    public int SplitNum = 0;
    public int Bet = 0;
    public int Payout = 0;
    public bool IsDoubleDown = false;
    public Result Result;
    public bool Dealer = false;
}

public enum Result : int
{
    Bust = 0,
    Win = 1,
    Draw = 2,
    Loss = 3,
    Waiting = 4,
    Blackjack = 5,
    Surrender = 6
}

public enum Card : int
{
    Number_2 = 2,
    Number_3 = 3,
    Number_4 = 4,
    Number_5 = 5,
    Number_6 = 6,
    Number_7 = 7,
    Number_8 = 8,
    Number_9 = 9,
    Ace = 1,
    Jack = 11,
    Queen = 12,
    King = 13,
    Number_10 = 10
}