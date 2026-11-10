using System;

class Probe {
    static void Main(string[] args) {
        try { Run(args); }
        catch (Exception ex) {
            Console.WriteLine("EX: " + ex.GetType().FullName + ": " + ex.Message);
            for (var e = ex.InnerException; e != null; e = e.InnerException)
                Console.WriteLine("  INNER: " + e.GetType().FullName + ": " + e.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }

    static void Run(string[] args) {
        int x = args.Length >= 2 ? int.Parse(args[0]) : 1670;
        int y = args.Length >= 2 ? int.Parse(args[1]) : 1952;

        var uia = Uia.Create();
        Console.WriteLine("uia=" + (uia != null));
        IntPtr tb = Uia.FindTaskbar();
        Console.WriteLine("taskbar hwnd=" + tb);
        Uia.IUIAutomationElement tbEl;
        uia.ElementFromHandle(tb, out tbEl);
        Console.WriteLine("taskbar element=" + (tbEl != null));
        if (tbEl == null) return;

        object cond;
        uia.CreateTrueCondition(out cond);
        var tbr = tbEl.get_CurrentBoundingRectangle();
        Console.WriteLine($"taskbar element rect=({tbr.left},{tbr.top})-({tbr.right},{tbr.bottom})");
        var all = tbEl.FindAll(Uia.TreeScope_Descendants, cond);
        int n = all.get_Length();
        Console.WriteLine("descendants=" + n);
        int buttons = 0, hits = 0;
        for (int i = 0; i < n; i++) {
            var e = all.GetElement(i);
            if (e == null) continue;
            int ct = e.get_CurrentControlType();
            if (ct != 50000) continue;   // Button
            buttons++;
            string name = e.get_CurrentName();
            string aid = e.get_CurrentAutomationId();
            var r = e.get_CurrentBoundingRectangle();
            bool hit = x >= r.left && x < r.right && y >= r.top && y < r.bottom;
            if (hit) hits++;
            if (hit || buttons <= 6)
                Console.WriteLine($"{(hit ? "HIT " : "    ")}name='{name}' aid='{aid}' rect=({r.left},{r.top})-({r.right},{r.bottom})");
        }
        Console.WriteLine($"buttons={buttons} hits={hits}");
    }
}
