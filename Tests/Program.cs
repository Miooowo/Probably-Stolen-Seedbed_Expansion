using System;
using GuestMaidenCorrupt;

static class Program
{
    static int failures;

    static void Main()
    {
        Check(GuestChance.Of(0, 0, 0, false) == 0, "零加成是 0%");
        Check(GuestChance.Of(1, 0, 0, false) == 2, "1 点正声望是 1%");
        Check(GuestChance.Of(37, 0, 0, false) == 74, "正声望 1 比 1");
        Check(GuestChance.Of(-4, 0, 0, false) == 0, "负声望低于 0 时钳到 0%");
        Check(GuestChance.Of(-4, 0, 0, true) == 22, "负声望先和使用过的精液相加再钳制");
        Check(GuestChance.Of(0, 0, 0, false) == 0, "没有对顾客使用精液时不加成功率");
        Check(GuestChance.Of(0, 0, 0, true) == 30, "对顾客用过精液额外 15%");
        Check(FailureCost.Crime(false) == 20, "失败犯罪基础 20");
        Check(FailureCost.Crime(true) == 17, "已使用精液时犯罪减少 15%");
        Check(Math.Abs(FailureCost.ReputationLoss(false, false) - 1) < 0.0000001, "失败声望基础扣 1");
        Check(Math.Abs(FailureCost.ReputationLoss(true, false) - 0.67) < 0.0000001, "已使用精液时声望扣 0.67");
        Check(Math.Abs(FailureCost.ReputationLoss(false, true) - 0.67) < 0.0000001, "凶神恶煞时声望扣 0.67");
        Check(Math.Abs(FailureCost.ReputationLoss(true, true) - 0.4489) < 0.0000001, "精液和凶神恶煞的声望减免相乘");
        Check(MaidenSellPrice.Adjust(0, 100, 1) == 90, "第 1 天没有售价时按主模组价格打九折");
        Check(MaidenSellPrice.Adjust(200, 100, 13) == 180, "第 13 天已有售价时打九折");
        Check(MaidenSellPrice.Adjust(0, 100, 14) == 100, "第 14 天不再降价");
        Check(MaidenSellPrice.Adjust(80, 100, 20) == 80, "第 14 天之后保留已写售价");
        Check(GuestChance.Of(90, 0, 0, true) == 200, "超过 100 钳到 100%");
        Check(GuestChance.Of(100, 0, 0, false) == 200, "100 声望是 100%");
        Check(GuestChance.Of(0, 1, 0, false) == 1, "1 点好感是 0.5%");
        Check(GuestChance.Of(0, 0, 1, false) == 1, "1 点幸运是 0.5%");
        Check(GuestChance.Of(0, -4, 0, false) == 0, "负好感低于 0 时钳到 0%");
        Check(GuestChance.Of(1, -4, 10, false) == 8, "声望 1、好感 -4、幸运 10 是 4%");
        Check(GuestChance.Of(0, 0, 0, false, 1) == 1, "1 点信任是 0.5%");
        Check(GuestChance.Of(0, 0, 0, false, -4) == 0, "负信任低于 0 时钳到 0%");
        Check(GuestChance.Of(1, -4, 10, false, 2) == 10, "声望 1、好感 -4、幸运 10、信任 2 是 5%");
        Check(GuestChance.Of(0, -3, 0, true, 4) == 31, "负信任先和使用过的精液相加再钳制");
        MaidenOutput.Shares plain = MaidenOutput.Weight(0, 0);
        Check(plain.Uncapped == 0 && plain.Capped == 5 && plain.Ruined == 38 && plain.Corrupt == 38 && plain.Normal == 19, "未喂养时无限制 0、受限 5、低质量 76、普通 19");
        MaidenOutput.Shares onePart = MaidenOutput.Weight(1, 0);
        Check(onePart.Uncapped == 0 && onePart.Capped == 5 && onePart.Ruined == 33 && onePart.Corrupt == 33 && onePart.Normal == 29, "喂 1 个电子元件后低质量占剩余的 70%");
        MaidenOutput.Shares twoCores = MaidenOutput.Weight(0, 2);
        Check(twoCores.Uncapped == 10 && twoCores.Capped == 5 && twoCores.Ruined == 34 && twoCores.Corrupt == 34 && twoCores.Normal == 17, "喂 2 个受限神经核心后无限制占 10%");
        MaidenOutput.Shares noLow = MaidenOutput.Weight(8, 0);
        Check(noLow.Ruined == 0 && noLow.Corrupt == 0 && noLow.Normal == 95 && noLow.Capped == 5, "喂 8 个电子元件后低质量降到 0");
        MaidenOutput.Shares cappedCore = MaidenOutput.Weight(0, 10);
        Check(cappedCore.Uncapped == 50 && cappedCore.Capped == 5 && cappedCore.Ruined == 18 && cappedCore.Corrupt == 18 && cappedCore.Normal == 9, "无限制神经核心封顶 50%");
        Check(MaidenOutput.Weight(0, 11).Uncapped == 50, "超过 10 次神经核心喂养仍封顶 50%");
        Check(MaidenOutput.Pick(plain, 0, 0) == MaidenOutput.CappedId, "未喂养时 0 落在受限神经核心");
        Check(MaidenOutput.Pick(plain, 4, 0) == MaidenOutput.CappedId, "未喂养时 4 仍是受限神经核心");
        Check(MaidenOutput.Pick(plain, 5, 0) == MaidenOutput.RuinedId, "未喂养时 5 是损坏模组");
        Check(MaidenOutput.Pick(plain, 80, 0) == MaidenOutput.CorruptId, "未喂养时 80 是腐化模组");
        Check(MaidenOutput.Pick(plain, 81, 3) == MaidenOutput.NormalIds[3], "未喂养时 81 起是普通模组");
        Check(MaidenOutput.Pick(cappedCore, 0, 0) == MaidenOutput.UncappedId, "封顶后 0 是无限制神经核心");
        Check(MaidenOutput.Pick(cappedCore, 49, 0) == MaidenOutput.UncappedId, "封顶后 49 仍是无限制神经核心");
        Check(MaidenOutput.Pick(cappedCore, 50, 0) == MaidenOutput.CappedId, "封顶后 50 是受限神经核心");
        Check(GuestChance.Of(0, -10, 0, true) == 20, "负好感先和使用过的精液相加再钳制");
        Check(GuestChance.Succeeds(40, 0), "20% 时 0 成功");
        Check(GuestChance.Succeeds(40, 39), "20% 时最后一档成功");
        Check(!GuestChance.Succeeds(40, 40), "20% 时下一档失败");
        Check(GuestChance.Succeeds(1, 0), "0.5% 时 0 成功");
        Check(!GuestChance.Succeeds(1, 1), "0.5% 时 1 失败");
        Check(!GuestChance.Succeeds(0, 0), "0% 必定失败");
        Check(GuestChance.Succeeds(200, 199), "100% 时最后一档成功");
        Check(GuestChance.Format(0) == "0%", "0 显示 0%");
        Check(GuestChance.Format(1) == "0.5%", "1 档显示 0.5%");
        Check(GuestChance.Format(8) == "4%", "8 档显示 4%");
        Check(GuestChance.Format(5) == "2.5%", "5 档显示 2.5%");
        Check(GuestChance.Format(200) == "100%", "满值显示 100%");
        if (failures != 0)
        {
            Environment.ExitCode = 1;
        }
    }

    static void Check(bool ok, string name)
    {
        if (ok)
        {
            Console.WriteLine("通过 " + name);
            return;
        }

        failures += 1;
        Console.WriteLine("失败 " + name);
    }
}
