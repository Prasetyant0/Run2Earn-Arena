namespace GameBalap.Core
{
    public enum RaceState
    {
        Idle,
        Matchmaking,
        Countdown,
        Racing,
        Finished,
        ResultDisplayed
    }

    public enum Lane
    {
        Left = -1,
        Middle = 0,
        Right = 1
    }

    public enum CharacterMovementState
    {
        Running,
        Jumping,
        Sliding,
        Stumbled,
        Finished
    }

    public enum ObstacleType
    {
        BarrierFull,       // Harus ganti lane (kiri/kanan)
        HurdleLow,         // Harus lompat (swipe up)
        HighBeam           // Harus slide/tunduk (swipe down)
    }

    public enum TransactionType
    {
        InitialGrant,      // Bonus koin awal instalasi
        DailyReward,       // Bonus harian testing
        RaceEntryFee,      // Biaya masuk balapan (debit)
        RaceReward,        // Hadiah kemenangan (kredit)
        TestFaucet,        // Tombol tambah koin virtual untuk testing
        TrackCoinPickup,   // Koin virtual yang diambil di lintasan
        ShopPurchase       // Pembelian item/skin/upgrade di toko
    }

    public enum RaceTier
    {
        Bronze = 100,      // Entry 100 coin, payout 2.0x (200 coin)
        Silver = 500,      // Entry 500 coin, payout 2.2x (1,100 coin)
        Gold = 1000        // Entry 1,000 coin, payout 2.5x (2,500 coin)
    }

    public enum EnvironmentTheme
    {
        CyberpunkNeon,     // Tema Kota Neon Cyberpunk (Default / Bronze)
        DesertDune,        // Tema Lembah Gurun Pasir Kuno (Silver)
        VolcanicMagma      // Tema Kawah Vulkanik Magma Membara (Gold)
    }
}
