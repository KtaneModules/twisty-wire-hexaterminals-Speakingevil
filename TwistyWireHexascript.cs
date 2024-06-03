using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class TwistyWireHexascript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public KMBombInfo info;
    public List<KMSelectable> terminals;
    public Transform[] spinners;
    public Renderer[] wires;
    public Renderer[] outwires;
    public Transform[] hatches;
    public Renderer[] sockets;
    public Material[] wmats;
    public Renderer[] timersegs;
    public Renderer strikeind;
    public Light[] bulbs;
    public Renderer[] ancwires;
    public Material[] awmats;
    public GameObject matstore;

    private readonly static int[][] network = new int[19][] { new int[6] { 34, 35, 36, 1, 4, 3 }, new int[6] { 0, 37, 38, 2, 5, 4 }, new int[6] { 1, 39, 40, 41, 6, 5 }, new int[6] { 32, 33, 0, 4, 8, 7 }, new int[6] { 3, 0, 1, 5, 9, 8 }, new int[6] { 4, 1, 2, 6, 10, 9 }, new int[6] { 5, 2, 42, 43, 11, 10 }, new int[6] { 30, 31, 3, 8, 12, 59 }, new int[6] { 7, 3, 4, 9, 13, 12 }, new int[6] { 8, 4, 5, 10, 14, 13 }, new int[6] { 9, 5, 6, 11, 15, 14 }, new int[6] { 10, 6, 44, 45, 46, 15 }, new int[6] { 58, 7, 8, 13, 16, 57 }, new int[6] { 12, 8, 9, 14, 17, 16 }, new int[6] { 13, 9, 10, 15, 18, 17 }, new int[6] { 14, 10, 11, 47, 48, 18 }, new int[6] { 56, 12, 13, 17, 54, 55 }, new int[6] { 16, 13, 14, 18, 52, 53 }, new int[6] { 17, 14, 15, 49, 50, 51 } };
    private readonly static int[][] configs = new int[14][] { new int[6]{ 4, 4, -1, -1, -1, -1 }, new int[6]{ 7, -1, 7, -1, -1, -1 }, new int[6] { 8, -1, -1, 8, -1, -1}, new int[6] { 0, 0, 1, 1, -1, -1}, new int[6] { 3, -1, 1, 1, -1, 3}, new int[6] { 5, -1, 5, -1, 2, 2}, new int[6] { 0, 0, -1, 6, -1, 6}, new int[6] { 8, -1, -1, 8, 2, 2}, new int[6] { 5, -1, 5, 6, -1, 6}, new int[6] { 5, 9, 5, -1, 9, -1}, new int[6] { 8, 9, -1, 8, 9, -1}, new int[6] { 0, 0, 1, 1, 2, 2}, new int[6] { 3, 9, 1, 1, 9, 3}, new int[6] { 5, 9, 5, 6, 9, 6} };
    private readonly static int[][] configshuff = new int[14][] { new int[] {3, 4, 5, 6, 7, 11, 12}, new int[] {5, 6, 8, 9, 13}, new int[] {7, 9, 10, 12, 13}, new int[] {3, 11}, new int[] {4, 12}, new int[] {5}, new int[] {6}, new int[] {7}, new int[] {8, 13}, new int[] {9, 13}, new int[] {10}, new int[] {11}, new int[] {12}, new int[] {13} };
    private readonly static bool[][] digidisps = new bool[10][] { new bool[7] { true, true, true, false, true, true, true}, new bool[7] { false, false, true, false, false, true, false}, new bool[7] { true, false, true, true, true, false, true}, new bool[7] { true, false, true, true, false, true, true}, new bool[7] { false, true, true, true, false, true, false}, new bool[7] { true, true, false, true, false, true, true}, new bool[7] { true, true, false, true, true, true, true}, new bool[7] { true, false, true, false, false, true, false}, new bool[7] { true, true, true, true, true, true, true}, new bool[7] { true, true, true, true, false, true, true}};
    private int[] termtypes = new int[19];
    private int[] rots = new int[19];
    private List<Vector2Int>[] connections = new List<Vector2Int>[19];
    private List<int>[] paths = new List<int>[3];
    private int[] targets = new int[3];
    private float delay = 60f;
    private static float tickspeed;
    private int timer;
    private bool spin;

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void Start()
    {
        moduleID = ++moduleIDCounter;
        matstore.SetActive(false);
        float scale = module.transform.lossyScale.x;
        foreach (Light b in bulbs)
            b.range *= scale;
        if (moduleID == moduleIDCounter)
            tickspeed = Mathf.Sqrt(info.GetModuleNames().Count(x => x == "Twisty Wire Hexaterminals") + (info.GetModuleNames().Count() - info.GetSolvableModuleNames().Count()) / 2);
        int[] ancwirarr = Enumerable.Range(0, 5).ToArray().Shuffle().ToArray();
        for (int i = 0; i < 4; i++)
            ancwires[i].material = awmats[ancwirarr[i]];
        delay += (moduleIDCounter - moduleID) * 20 * tickspeed;
        foreach(KMSelectable button in terminals)
        {
            int b = terminals.IndexOf(button);
            button.OnInteract += delegate ()
            {
                if (!moduleSolved && !spin)
                {
                    int s = 0;
                    for(int i = 0; i < targets.Count(x => x >= 0); i++)
                        if (paths[i].Contains(b))
                        {
                            s = 1;
                            if(paths[i].Last() == targets[i])
                            {
                                s = 2;
                                outwires[i].material = wmats[i];
                                outwires[i + 3].material = wmats[4];
                            }
                        }
                    Audio.PlaySoundAtTransform("Off" + s, spinners[b]);
                    StartCoroutine(Spin(b));
                }
                return false;
            };
        }
        info.OnBombExploded += delegate ()
        {
            StopAllCoroutines();
            StartCoroutine(Solve());
        };
        info.OnBombSolved += delegate ()
        {
            StopAllCoroutines();
            StartCoroutine(Solve());
        };
        module.HandlePass();
        StartCoroutine(Activate());
    }

    private IEnumerator Activate()
    {
        yield return new WaitForSeconds(delay);
        Setup();
    }

    private void Setup()
    {
        foreach (Renderer w in wires)
        {
            w.enabled = true;
            w.material = wmats[4];
        }
        regen:
        for (int i = 0; i < 19; i++)
            connections[i] = new List<Vector2Int> { };
        int[] inputs = new int[3];
        int[] rollback = new int[2] { 1, 1 };
        for(int i = 0; i < 3; i++)
        {
        path:
            do {
                inputs[i] = Random.Range(30, 60);
            } while (targets.Contains(inputs[i]) || inputs.Take(i).Select(x => Mathf.Abs(x - inputs[i])).Any(x => x < 3 || x > 27));
            paths[i] = new List<int> { inputs[i] };
            int d = 0;
            for (int j = 0; j < 19; j++)
                if (network[j].Contains(inputs[i]))
                {
                    paths[i].Add(j);
                    for (int k = 0; k < 6; k++)
                        if (network[j][k] == inputs[i])
                        {
                            d = k;
                            break;
                        }
                    break;
                }
            while(paths[i].Last() < 30)
            {
                int t = paths[i].Last();
                List<int> dir = new List<int> { 0, 1, 2, 3, 4, 5};
                for(int j = 0; j < 6; j++)
                {
                    if (j == d || (network[t][j] > 29 && (paths[i].Count() < 6 || Mathf.Abs(network[t][j] - paths[i][0]) < 4 || Mathf.Abs(network[t][j] - paths[i][0]) > 26)) || inputs.Take(i).Contains(network[t][j]) || targets.Take(i).Contains(network[t][j]) || connections[t].Any(x => j == x.x || j == x.y) || (network[t][j] < 30 && (connections[network[t][j]].Count() > 2 || connections[network[t][j]].Count(x => Mathf.Abs(x.x - x.y) == 3) > 1)) || connections[t].Any(x => Mathf.Abs(x.x - x.y) % 2 == 0 && Mathf.Abs(x.x - j) % 4 == 1 && Mathf.Abs(x.y - j) % 4 == 1) || (connections[t].Any(x => Mathf.Abs(x.x - d) % 4 == 1 && Mathf.Abs(x.y - d) % 4 == 1) && j != (d + 3) % 6))
                        dir.Remove(j);
                }
                if (dir.Count() < 1)
                {
                    if (rollback[0] > paths[i].Count() - 2)
                    {
                        paths[i] = paths[i].Take(1).ToList();
                        goto path;
                    }
                    else
                    {
                        paths[i] = paths[i].Take(paths[i].Count() - rollback[0]).ToList();
                        rollback[0]++;
                        rollback[1] = 1;
                    }
                }
                else
                {
                    if (rollback[1] < rollback[0])
                        rollback[1]++;
                    else
                        rollback = new int[2] { 1, 1 };
                    int r = dir.PickRandom();
                    Vector2Int wire = new Vector2Int(d, r);
                    if(connections[t].All(x => x.x != d || x.y != r))
                        connections[t].Add(wire);
                    paths[i].Add(network[t][r]);
                    d = (r + 3) % 6;
                }
            }
            targets[i] = paths[i].Last();
            if(paths.Take(i + 1).Sum(x => x.Count()) > 12 + (5 * i))
            {
                for(int j = i + 1; j < 3; j++)
                {
                    paths[j] = new List<int> { -2 };
                    targets[j] = -1;
                }
                break;
            }
        }
        for (int i = 0; i < 19; i++)
        {
            termtypes[i] = -1;
            if (connections[i].Count() < 1)
                termtypes[i] = Random.Range(0, 3);
            else
            {
                Vector2Int[] c = connections[i].ToArray();
                for (int j = 0; j < 84; j++)
                {
                    Vector2Int[] r = c.Select(x => new Vector2Int(configs[j / 6][(x.x + j) % 6], configs[j / 6][(x.y + j) % 6])).ToArray();
                    if(r.All(x => x.x >= 0 && x.y >= 0 && x.x == x.y))
                    {
                        termtypes[i] = j / 6;
                        break;
                    }
                }
                if (termtypes[i] < 0)
                    goto regen;
            }
            if (Random.Range(0, 2) == 0)
                termtypes[i] = configshuff[termtypes[i]].PickRandom();
            for (int j = 0; j < 10; j++)
                wires[(i * 10) + j].enabled = configs[termtypes[i]].Contains(j);
            connections[i].Clear();
            int ro = Random.Range(0, 6);
            rots[i] = ro;
            for (int j = 0; j < 5; j++)
                for (int k = j + 1; k < 6; k++)
                {
                    Vector2Int c = new Vector2Int(configs[termtypes[i]][j], configs[termtypes[i]][k]);
                    if (c.x >= 0 && c.y >= 0 && c.x == c.y)
                    {
                        connections[i].Add(new Vector2Int((j + ro) % 6, (k + ro) % 6));
                        break;
                    }
                }
        }
        PathUpdate(false);
        while (paths.Where((x, q) => (targets[q] >= 0 && targets[q] == x.Last()) || paths.Where((y, p) => y.Count() > 1 && p != q).Any(y => x.Last() == y[0])).Any())
        {
            for (int i = 0; i < 19; i++)
            {
                int ro = Random.Range(0, 6);
                rots[i] += ro;
                rots[i] %= 6;
                connections[i] = connections[i].Select(x => new Vector2Int((x.x + ro) % 6, (x.y + ro) % 6)).ToList();
            }
            PathUpdate(false);
        }
        for (int i = 0; i < 19; i++)
            spinners[i].localEulerAngles = new Vector3(-90, 0, 60 * rots[i]);
        StartCoroutine(HatchMove(true));
    }

    private void PathUpdate(bool rend)
    {
        if (rend)
        {
            foreach (Renderer w in wires)
                w.material = wmats[4];
            for (int i = 0; i < 30; i++)
                sockets[i].material = wmats[4];
        }
        paths = paths.Select(x => x.Take(1).ToList()).ToArray();
        int t = 0;
        int d = 0;
        for (int i = 0; i < targets.Count(x => x >= 0); i++)
        {
            int p = paths[i][0];
            List<int> rends = new List<int>{ };
            if (rend)
                sockets[p - 30].material = wmats[i];
            for (int j = 0; j < 19; j++)
                if (network[j].Contains(p))
                {
                    t = j;
                    for (int k = 0; k < 6; k++)
                        if (network[j][k] == p)
                        {
                            d = k;
                            break;
                        }
                    break;
                }
            while(t < 30 && connections[t].Any(x => x.x == d || x.y == d))
            {
                for (int j = 0; j < connections[t].Count(); j++)
                {
                    if(connections[t][j].x == d)
                    {
                        d = connections[t][j].y;
                        break;
                    }
                    else if(connections[t][j].y == d)
                    {
                        d = connections[t][j].x;
                        break;
                    }
                }
                paths[i].Add(t);
                if(rend && t < 30)
                    rends.Add((t * 10) + configs[termtypes[t]][(d - rots[t] + 6) % 6]);
                t = network[t][d];
                d += 3;
                d %= 6;
            }
            if (t >= 30)
                paths[i].Add(t);
            if (rend)
            {
                int r = paths.Any(x => x.Count() > 1 && x[0] == paths[i].Last()) ? 3 : i;
                for (int j = 0; j < rends.Count(); j++)
                    wires[rends[j]].material = wmats[r];
                if (t >= 30)
                    sockets[t - 30].material = wmats[r];
                if(r == 3)
                {
                    StopAllCoroutines();
                    for(int j = 0; j < 3; j++)
                    {
                        outwires[j].material = wmats[3];
                        outwires[j + 3].material = wmats[4];
                    }
                    StartCoroutine(EmergencyStop());
                }
            }
        }
    }

    private IEnumerator HatchMove(bool o)
    {
        Audio.PlaySoundAtTransform("Hatch" + (o ? "Open" : "Close"), transform);
        Vector2 e = new Vector2(0, 1);
        float q = 0;
        bulbs[0].enabled = o;
        if (o)
        {
            int t = targets.Count(x => x >= 0);
            timer = (t * 20) + 39;
            for (int i = 0; i < t; i++)
                outwires[i].material = wmats[i];
        }
        while(e.x < e.y)
        {
            if (o)
            {
                e.x += Time.deltaTime;
                q = e.x * (2 - e.x);
                bulbs[0].intensity = e.x * 300;
                for (int i = 0; i < 6; i++)
                {
                    hatches[i].localEulerAngles = new Vector3(Mathf.Lerp(-90, -206, q), 0, 0);
                    hatches[i + 6].localEulerAngles = new Vector3(60 * q, 0, 0);
                }
            }
            else
            {
                e.y -= Time.deltaTime;
                q = e.y * e.y;
                for (int i = 0; i < 6; i++)
                {
                    hatches[i].localEulerAngles = new Vector3(Mathf.Lerp(-90, -206, q), 0, 0);
                    hatches[i + 6].localEulerAngles = new Vector3(60 * q, 0, 0);
                }
            }
            yield return null;
        }
        for (int i = 0; i < 6; i++)
        {
            hatches[i].localEulerAngles = new Vector3(o ? -206 : -90, 0, 0);
            hatches[i + 6].localEulerAngles = new Vector3(o ? 60 : 0, 0 ,0);
        }
        if (o)
        {
            for(int i = 0; i < targets.Count(x => x >= 0); i++)
            {
                sockets[paths[i][0]].material = wmats[3];
                sockets[targets[i]].material = wmats[i];
            }
            PathUpdate(true);
            StartCoroutine(Countdown());
        }
        else
        {
            yield return new WaitForSeconds(Random.Range(40f, 80f) * tickspeed);
            Setup();
        }
    }

    private IEnumerator Countdown()
    {
        moduleSolved = false;
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.NeedyActivated, timersegs[4].transform);
        for (int i = 0; i < targets.Count(x => x >= 0); i++)
            outwires[i].material = wmats[i];
        for(int i = timer; i >= 0; i--)
        {
            for(int j = 0; j < 7; j++)
            {
                timersegs[j].enabled = digidisps[i / 10][j];
                timersegs[j + 7].enabled = digidisps[i % 10][j];
            }
            for (int j = 0; j < 2; j++)
                bulbs[j].intensity = 1000 - ((700f * i) / timer);
            yield return new WaitForSeconds(tickspeed);
        }
        for (int j = 0; j < 7; j++)
        {
            timersegs[j].enabled = j == 3;
            timersegs[j + 7].enabled = j == 3;
        }
        foreach (Renderer w in wires)
            w.material = wmats[4];
        foreach (Renderer w in outwires)
            w.material = wmats[4];
        for (int i = 0; i < 30; i++)
        {
            sockets[i].material = wmats[4];
            sockets[i + 30].material = wmats[5];
        }
        if (!moduleSolved)
            StartCoroutine(Strike());
        moduleSolved = true;
        bulbs[1].enabled = false;
        StartCoroutine(HatchMove(false));
    }

    private IEnumerator Spin(int b)
    {
        spin = true;
        List<Vector2Int> s = new List<Vector2Int> { };
        for(int i = 0; i < connections[b].Count(); i++)
        {
            Vector2Int x = connections[b][i];
            s.Add(new Vector2Int((x.x + 1) % 6, (x.y + 1) % 6));
        }
        connections[b].Clear();
        PathUpdate(true);
        float e = 0;
        while(e < 0.15f)
        {
            e += Time.deltaTime;
            spinners[b].localEulerAngles = new Vector3(-90, 0, Mathf.Lerp(rots[b] * 60, (rots[b] * 60) + 60, e / 0.15f));
            yield return null;
        }
        rots[b]++;
        rots[b] %= 6;
        spinners[b].localEulerAngles = new Vector3(-90, 0, (rots[b] * 60) % 360);
        connections[b] = s;
        PathUpdate(true);
        int t = targets.Count(x => x >= 0);
        int[] c = new int[t];
        for (int i = 0; i < t; i++)
            if (paths[i].Contains(b))
            {
                c[i] = 1;
                if (paths[i].Last() == targets[i])
                {
                    c[i] = 2;
                    outwires[i].material = wmats[4];
                    outwires[i + 3].material = wmats[i];
                }
            }
        if (paths.Where((x, q) => x.Last() == targets[q]).Count() == t)
        {
            moduleSolved = true;
            bulbs[0].enabled = false;
            bulbs[1].enabled = true;
        }
        Audio.PlaySoundAtTransform("On" + (int)Mathf.Max(c), spinners[b]);
        spin = false;
    }

    private IEnumerator EmergencyStop()
    {
        moduleSolved = true;
        bulbs[0].intensity = 1000;
        foreach (Renderer d in timersegs)
            d.enabled = false;
        Audio.PlaySoundAtTransform("Emergency", transform);
        terminals[8].AddInteractionPunch(1);
        yield return new WaitForSeconds(1);
        StartCoroutine("EmergencyFlash");
        float e = 0;
        while(e < 0.5f)
        {
            e += Time.deltaTime;
            for (int i = 0; i < 6; i++)
            {
                hatches[i].localEulerAngles = new Vector3(Mathf.Lerp(-206, -90, e * 2), 0, 0);
                hatches[i + 6].localEulerAngles = new Vector3(Mathf.Lerp(60, -120, e * 2), 0, 0);
            }
            yield return null;
        }
        for (int i = 0; i < 6; i++)
            hatches[i].localEulerAngles = new Vector3(-90, 0, 0);
        while (e > 0)
        {
            e -= Time.deltaTime;
            for (int i = 6; i < 12; i++)
                hatches[i].localEulerAngles = new Vector3(-240 * e, 0, 0);
            yield return null;
        }
        for (int i = 6; i < 12; i++)
            hatches[i].localEulerAngles = new Vector3(0, 0, 0);
        for (int i = 0; i < 30; i++)
        {
            sockets[i].material = wmats[4];
            sockets[i + 30].material = wmats[5];
        }
        float[] t = new float[5];
        for (int i = 0; i < 5; i++)
        {
            t[i] = Random.Range(0.1f, 0.4f);
            terminals.PickRandom().AddInteractionPunch(t[i] * 2.5f);
            yield return new WaitForSeconds(t[i]);
        }
        yield return new WaitForSeconds(3 - t.Sum());
        terminals.PickRandom().AddInteractionPunch(2);
        foreach (Renderer w in wires)
            w.material = wmats[4];
        StopCoroutine("EmergencyFlash");
        for (int i = 0; i < 3; i++)
            outwires[i].material = wmats[4];
        bulbs[0].enabled = false;
        StartCoroutine(Strike());
        yield return new WaitForSeconds(Random.Range(30f, 60f) * tickspeed);
        Setup();
    }

    private IEnumerator EmergencyFlash()
    {
        float e = 0;
        while (true)
        {
            e += Time.deltaTime;
            float a = 1 - Mathf.Abs(Mathf.Cos(e * 3 * Mathf.PI));
            bulbs[0].intensity = 1000 * a;
            yield return null;
        }
    }

    private IEnumerator Strike()
    {
        module.HandleStrike();
        yield return null;
        foreach (Renderer d in timersegs)
            d.enabled = false;
        strikeind.enabled = true;
        yield return new WaitForSeconds(1.25f);
        strikeind.enabled = false;
        yield return new WaitForSeconds(0.25f);
        for (int i = 0; i < 14; i++)
            timersegs[i].enabled = i % 7 == 3;
    }

    private IEnumerator Solve()
    {
        moduleSolved = true;
        strikeind.enabled = false;
        foreach (Renderer d in timersegs)
            d.enabled = false;
        foreach (Renderer w in wires)
            w.material = wmats[4];
        foreach (Renderer w in outwires)
            w.material = wmats[4];
        for (int i = 0; i < 2; i++)
            bulbs[i].enabled = i == 1;
        bulbs[1].intensity = 1000;
        Audio.PlaySoundAtTransform("HatchClose", transform);
        for(int i = 0; i < 30; i++)
        {
            sockets[i].material = wmats[4];
            sockets[i + 30].material = wmats[5];
        }
        float e = -hatches[0].localEulerAngles.x / 126;
        while(e > 0)
        {
            e -= Time.deltaTime;
            float q = e * e;
            for (int i = 0; i < 6; i++)
            {
                hatches[i].localEulerAngles = new Vector3(Mathf.Lerp(-90, -206, q), 0, 0);
                hatches[i + 6].localEulerAngles = new Vector3(60 * q, 0, 0);
            }
            yield return null;
        }
    }
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} <A-E><1-5> <1-5> [Rotates terminals the given numbers of times.Rows are numbered 1-5 from top to bottom, columns are labeled A-E from left to right, from the given row.Chain with spaces.]";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string cmd)
    {
        var intCmd = cmd.Trim();
        var rgxValidCoordinate = Regex.Match(intCmd, @"^(\s?[A-E][1-5](\s\d+)?)+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var matchIdxesGroup = new int[][] {
                new[] { 0, 1, 2 },
                new[] { 3, 4, 5, 6 },
                new[] { 7, 8, 9, 10, 11 },
                new[] { 12, 13, 14, 15 },
                new[] { 16, 17, 18 },
            };
        if (rgxValidCoordinate.Success)
        {
            var matchingValuesSplitted = rgxValidCoordinate.Value.Split();
            var idxItemsToSelect = new List<int>();
            var amountToSpin = new List<int>();
            foreach (var matchedVal in matchingValuesSplitted)
            {
                if (Regex.IsMatch(matchedVal, @"^[A-E][1-5]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    var idxLetter = "ABCDE".IndexOf(matchedVal.ToUpperInvariant().First());
                    var idxNumeral = "12345".IndexOf(matchedVal.ToUpperInvariant().Last());
                    if (matchIdxesGroup[idxNumeral].Length <= idxLetter)
                    {
                        yield return string.Format("sendtochaterror There is no terminal at position {0}.", matchedVal);
                        yield break;
                    }
                    if (amountToSpin.Count < idxItemsToSelect.Count)
                        amountToSpin.Add(1);
                    idxItemsToSelect.Add(matchIdxesGroup[idxNumeral][idxLetter]);
                }
                else if (Regex.IsMatch(matchedVal, @"^\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                {
                    int valueToSpin;
                    if (!int.TryParse(matchedVal, out valueToSpin) || valueToSpin % 6 <= 0)
                    {
                        yield return string.Format("sendtochaterror Terminal cannot be rotated {0} times.", matchedVal);
                        yield break;
                    }
                    amountToSpin.Add(valueToSpin % 6);
                }
                else
                    yield break;
            }
            if (amountToSpin.Count < idxItemsToSelect.Count)
                amountToSpin.Add(1);
            yield return null;
            for (var x = 0; x < idxItemsToSelect.Count; x++)
            {
                for (var y = 0; y < amountToSpin[x]; y++)
                {
                    terminals[idxItemsToSelect[x]].OnInteract();
                    yield return string.Format("trywaitcancel 0.1 Spinning the terminals has been canceled after attempting to adjust a terminal {0} time(s)!", y);
                    while (spin)
                        yield return string.Format("trycancel Spinning the terminals has been canceled after attempting to adjust a terminal {0} time(s)!", y);
                }
            }
        }
        yield break;
    }
}
