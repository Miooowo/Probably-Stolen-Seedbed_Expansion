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
        Check(GuestChance.ChargeHalf(100) == 10, "100 淫力是 5%");
        Check(GuestChance.ChargeHalf(0) == -100, "0 淫力比 100 少 50%");
        Check(GuestChance.ChargeHalf(99) == 8, "99 淫力是 4%");
        Check(GuestChance.ChargeHalf(101) == 11, "101 淫力是 5.5%");
        Check(GuestChance.ChargeHalf(90) == -1, "90 淫力是 -0.5%");
        Check(GuestChance.ChargeHalf(200) == 120, "200 淫力是 60%");
        Check(GuestChance.Of(0, 0, 0, false, 0, 100) == 10, "只有 100 淫力时成功率是 5%");
        Check(GuestChance.Of(0, 0, 0, false, 0, 0) == 0, "0 淫力把负加成钳到 0%");
        Check(GuestChance.Of(0, 0, 0, false, 0, 110) == 21, "110 淫力是 10.5%");
        Check(GuestChance.Of(1, -4, 10, false, 0, 100) == 18, "原有 4% 再加 100 淫力的 5%");
        Check(MaidenOutput.Due(0, 0, 2).Count == 0, "未喂满时第 2 夜不产出");
        Check(MaidenOutput.Due(5, 0, 1).Count == 0, "未到间隔夜不产出");
        var capped = MaidenOutput.Due(5, 0, 2);
        Check(capped.Count == 1 && capped[0] == MaidenOutput.CappedId, "喂满 5 个电子零件后每 2 夜产出受限神经核心");
        var both = MaidenOutput.Due(5, 5, 4);
        Check(both.Count == 2 && both[0] == MaidenOutput.CappedId && both[1] == MaidenOutput.UncappedId, "再喂满 5 个受限神经核心后额外产出无限制神经核心");
        var coresOnly = MaidenOutput.Due(4, 5, 2);
        Check(coresOnly.Count == 1 && coresOnly[0] == MaidenOutput.UncappedId, "零件未满时只按已喂满的神经核心产出");
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
