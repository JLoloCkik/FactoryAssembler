using System;
using System.Collections.Generic;

namespace FactoryAssembler;

public class VirtualMachine
{
    // --- MEMÓRIA ÉS ÁLLAPOT ---

    // Regiszterek (változók): AX, BX, CX, DX
    public Dictionary<string, int> Registers { get; private set; } = new Dictionary<string, int>();

    // Stack (Verem) a PUSH/POP utasításokhoz
    private Stack<int> StackMemory = new Stack<int>();
    public bool IsWaiting { get; private set; } = false;

    // A programkód sorai
    public List<string> Instructions { get; private set; } = new List<string>();

    // Címkék (pl. LOOP:) helye a kódban
    private Dictionary<string, int> Labels = new Dictionary<string, int>();

    // Instruction Pointer (IP): Hol tartunk a futásban?
    public int IP { get; private set; } = 0;

    // Összehasonlítás eredménye (-1: kisebb, 0: egyenlő, 1: nagyobb)
    private int CompareFlag = 0;

    // Bemeneti és Kimeneti szalag (Queue = Sor)
    public Queue<int> InputBuffer { get; private set; } = new Queue<int>();
    public Queue<int> OutputBuffer { get; private set; } = new Queue<int>();

    // Hibaüzenet, ha valami elromlik (pl. 0-val osztás)
    public string LastError { get; private set; } = "";
    public bool IsHalted { get; private set; } = false;

    public VirtualMachine()
    {
        Reset();
    }

    public void Reset()
    {
        Registers["AX"] = 0;
        Registers["BX"] = 0;
        Registers["CX"] = 0;
        Registers["DX"] = 0; // Általános regiszterek
        Registers["TX"] = 0; // Ideiglenes regiszter (célzott műveletekhez)

        StackMemory.Clear();
        IP = 0;
        CompareFlag = 0;
        LastError = "";
        IsHalted = false;
        // A Puffereket nem töröljük, mert azok a kártyákhoz tartoznak!
    }

    // Kód betöltése és előkészítése (fordítás)
    public void LoadCode(string rawCode)
    {
        Instructions.Clear();
        Labels.Clear();

        if (string.IsNullOrWhiteSpace(rawCode)) return;

        string[] lines = rawCode.Split('\n');
        int instructionIndex = 0;

        foreach (var line in lines) {
            // Kommentek levágása (pl. "MOV AX, 1 # ez egy komment")
            string cleanLine = line.Split('#')[0].Trim().ToUpper();

            if (string.IsNullOrEmpty(cleanLine)) continue;

            // Címke kezelése (pl. "START:")
            if (cleanLine.EndsWith(":")) {
                string labelName = cleanLine.TrimEnd(':');
                Labels[labelName] = instructionIndex;
            }
            else {
                Instructions.Add(cleanLine);
                instructionIndex++;
            }
        }

        IP = 0;
        IsHalted = false;
    }

    // EGY LÉPÉS VÉGREHAJTÁSA (Ezt hívja majd a játék loop)
    public void Step()
    {
        if (IsHalted || IP >= Instructions.Count) return;

        try {
            string line = Instructions[IP];
            // Szóköz és vessző mentén darabolunk
            string[] parts = line.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            string opcode = parts[0];

            ExecuteOpcode(opcode, parts);

            // Ha nem volt ugrás (JMP), lépjünk a következő sorra
            if (!opcode.StartsWith("J")) {
                IP++;
            }
            else {
                // JMP, JE, JNE utasításoknál az ExecuteOpcode kezeli az IP-t
            }
        }
        catch (Exception ex) {
            LastError = $"Error at line {IP}: {ex.Message}";
            IsHalted = true;
        }
    }

    private void ExecuteOpcode(string opcode, string[] parts)
    {
        switch (opcode) {
            // --- ADATMOZGATÁS ---
            case "MOV": // MOV AX, 5
                Registers[parts[1]] = GetValue(parts[2]);
                break;

            // --- MATEK ---
            case "ADD": // ADD AX, BX  -> AX = AX + BX
                Registers[parts[1]] += GetValue(parts[2]);
                break;
            case "SUB": // SUB AX, 1
                Registers[parts[1]] -= GetValue(parts[2]);
                break;
            case "MUL":
                Registers[parts[1]] *= GetValue(parts[2]);
                break;
            case "DIV":
                int divVal = GetValue(parts[2]);
                if (divVal == 0) throw new Exception("Division by Zero");
                Registers[parts[1]] /= divVal;
                break;

            // --- LOGIKA ÉS UGRÁS ---
            case "CMP": // CMP AX, 10
                int v1 = GetValue(parts[1]);
                int v2 = GetValue(parts[2]);
                CompareFlag = v1.CompareTo(v2); // 0 ha egyenlő
                break;

            case "JMP": // Mindig ugrik
                IP = Labels[parts[1]];
                break;
            case "JE": // Jump if Equal (ha CMP eredménye 0)
                if (CompareFlag == 0) IP = Labels[parts[1]];
                else IP++;
                break;
            case "JNE": // Jump if Not Equal
                if (CompareFlag != 0) IP = Labels[parts[1]];
                else IP++;
                break;
            case "JL": // Jump if Less
                if (CompareFlag < 0) IP = Labels[parts[1]];
                else IP++;
                break;
            case "JG": // Jump if Greater
                if (CompareFlag > 0) IP = Labels[parts[1]];
                else IP++;
                break;

            // --- INPUT / OUTPUT ---
            case "IN":
                if (InputBuffer.Count > 0) {
                    Registers[parts[1]] = InputBuffer.Dequeue();
                    IsWaiting = false; // Van adat, megyünk tovább
                }
                else {
                    IsWaiting = true; // Nincs adat, blokkolunk!
                    return; // FONTOS: A return miatt nem fut le az IP++, így ezen a soron marad!
                }

                break;
            case "OUT": // OUT AX -> Beteszi a kimeneti sorba
                OutputBuffer.Enqueue(GetValue(parts[1]));
                break;

            // --- STACK ---
            case "PUSH":
                StackMemory.Push(GetValue(parts[1]));
                break;
            case "POP":
                if (StackMemory.Count > 0)
                    Registers[parts[1]] = StackMemory.Pop();
                else
                    throw new Exception("Stack Underflow");
                break;

            default:
                throw new Exception($"Unknown opcode: {opcode}");
        }
    }

    // Segédfüggvény: Számot vagy Regiszter értékét adja vissza
    private int GetValue(string param)
    {
        // Ha szám (pl. "42" vagy "-5")
        if (int.TryParse(param, out int val)) return val;
        // Ha regiszter (pl. "AX")
        if (Registers.ContainsKey(param)) return Registers[param];

        throw new Exception($"Invalid parameter: {param}");
    }
    // --- ÚJ: FELADAT TESZTELÉSE ÉS VALIDÁLÁSA ---
    public bool RunTest(List<int> expectedInputs, List<int> expectedOutputs, out string failReason)
    {
        // Mentsük el a jelenlegi állapotot (mert ez csak teszt)
        int savedIP = IP;
        var savedRegisters = new Dictionary<string, int>(Registers);
        
        // Reseteljük a teszthez
        Reset();
        
        // Feltöltjük a teszt bemenettel
        foreach (var val in expectedInputs) InputBuffer.Enqueue(val);
        
        int maxSteps = 1000; // Végtelen ciklus elleni védelem
        int steps = 0;
        
        // Futtatjuk, amíg meg nem kapjuk a várt számú kimenetet, vagy el nem fogy a lépés
        while (OutputBuffer.Count < expectedOutputs.Count && steps < maxSteps)
        {
            if (IsHalted) break;
            
            // Ha vár bemenetre, de már nincs mit beolvasni, és még nincs kész
            if (IsWaiting && InputBuffer.Count == 0) break; 
            
            Step();
            steps++;
        }

        bool success = true;
        failReason = "";

        // Eredmények ellenőrzése
        if (OutputBuffer.Count < expectedOutputs.Count)
        {
            success = false;
            failReason = $"Not enough outputs produced.\nProduced: {OutputBuffer.Count}, Expected: {expectedOutputs.Count}";
        }
        else
        {
            int i = 0;
            foreach (var outVal in OutputBuffer)
            {
                if (i >= expectedOutputs.Count) break; // Többet adott ki, mint kellene (ez lehet hiba is, most elnézzük)
                
                if (outVal != expectedOutputs[i])
                {
                    success = false;
                    failReason = $"Output mismatch at index {i}.\nGot: {outVal}, Expected: {expectedOutputs[i]}";
                    break;
                }
                i++;
            }
        }

        if (IsHalted && !string.IsNullOrEmpty(LastError))
        {
            success = false;
            failReason = $"Runtime Error: {LastError}";
        }

        // Állapot visszaállítása (hogy a játékban ott folytassa, ahol abbahagyta)
        IP = savedIP;
        Registers = savedRegisters;
        OutputBuffer.Clear();
        InputBuffer.Clear();

        return success;
    }
}