using System;
using System.Collections.Generic;

namespace FactoryAssembler;

public class VirtualMachine
{
    public Action<int>? OnOutput; 
    
    public Dictionary<string, int> Registers { get; private set; } = new Dictionary<string, int>();
    public List<string> Instructions { get; private set; } = new List<string>();
    private Dictionary<string, int> Labels = new Dictionary<string, int>();
    public int IP { get; private set; } = 0;
    private int CompareFlag = 0;
    public Queue<int> InputBuffer { get; private set; } = new Queue<int>();
    public Queue<int> OutputBuffer { get; private set; } = new Queue<int>();
    public bool IsHalted { get; private set; } = false;
    public bool IsWaiting { get; private set; } = false;
    public string LastError { get; private set; } = "";

    public VirtualMachine() { Reset(); }

    public void Reset()
    {
        Registers["AX"] = 0; Registers["BX"] = 0; Registers["CX"] = 0; Registers["DX"] = 0;
        IP = 0; CompareFlag = 0; IsHalted = false; IsWaiting = false; LastError = "";
    }

    public void LoadCode(string rawCode)
    {
        Instructions.Clear(); Labels.Clear();
        if (string.IsNullOrWhiteSpace(rawCode)) return;
        
        string[] lines = rawCode.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        int realLineIndex = 0;
        
        foreach (var line in lines)
        {
            string cleanLine = line.Split('#')[0].Trim().ToUpper();
            if (string.IsNullOrEmpty(cleanLine)) continue;
            
            // Címke mentése, magát a sort megtartjuk (hogy a sortörés számolás jó legyen)
            if (cleanLine.EndsWith(":")) 
            {
                Labels[cleanLine.TrimEnd(':')] = realLineIndex;
            }
            Instructions.Add(cleanLine); 
            realLineIndex++;
        }
        IP = 0; IsHalted = false; LastError = "";
    }

    public void Step()
    {
        if (IsHalted || Instructions.Count == 0) return;
        
        if (IP >= Instructions.Count) 
        {
            IP = 0; 
            return; 
        }

        try
        {
            string line = Instructions[IP];
            string[] parts = line.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length == 0) 
            {
                IP++;
                return;
            }

            string opcode = parts[0].Trim();

            // Ha ez egy címke sor (pl "START:"), lépjünk tovább
            if (opcode.EndsWith(":"))
            {
                IP++;
                return;
            }

            ExecuteOpcode(opcode, parts);
            
            if (!opcode.StartsWith("J") && !IsWaiting) 
            {
                IP++;
            }
        }
        catch (Exception ex) 
        { 
            LastError = ex.Message; 
            IsHalted = true; 
        }
    }

    private void ExecuteOpcode(string opcode, string[] parts)
    {
        switch (opcode)
        {
            case "MOV": Registers[parts[1].Trim()] = GetValue(parts[2].Trim()); break;
            case "ADD": Registers[parts[1].Trim()] += GetValue(parts[2].Trim()); break;
            case "SUB": Registers[parts[1].Trim()] -= GetValue(parts[2].Trim()); break;
            case "MUL": Registers[parts[1].Trim()] *= GetValue(parts[2].Trim()); break;
            case "DIV": 
                int d = GetValue(parts[2].Trim()); 
                if(d == 0) throw new Exception("Division by Zero!"); 
                Registers[parts[1].Trim()] /= d; 
                break;
            case "CMP": 
                int v1 = GetValue(parts[1].Trim()); 
                int v2 = GetValue(parts[2].Trim()); 
                CompareFlag = v1.CompareTo(v2); 
                break;
            case "JMP": 
                string lblJmp = parts[1].Trim();
                if(Labels.ContainsKey(lblJmp)) IP = Labels[lblJmp]; 
                else throw new Exception($"Label '{lblJmp}' not found"); 
                break;
            case "JE": 
                string lblJe = parts[1].Trim();
                if(CompareFlag == 0 && Labels.ContainsKey(lblJe)) IP = Labels[lblJe]; 
                else if(CompareFlag == 0) throw new Exception($"Label '{lblJe}' not found"); 
                else IP++; 
                break;
            case "JG": 
                string lblJg = parts[1].Trim();
                if(CompareFlag > 0 && Labels.ContainsKey(lblJg)) IP = Labels[lblJg]; 
                else if(CompareFlag > 0) throw new Exception($"Label '{lblJg}' not found"); 
                else IP++; 
                break;
            case "JL": 
                string lblJl = parts[1].Trim();
                if(CompareFlag < 0 && Labels.ContainsKey(lblJl)) IP = Labels[lblJl]; 
                else if(CompareFlag < 0) throw new Exception($"Label '{lblJl}' not found"); 
                else IP++; 
                break;
            case "IN": 
                if(InputBuffer.Count > 0) { Registers[parts[1].Trim()] = InputBuffer.Dequeue(); IsWaiting = false; } 
                else { IsWaiting = true; return; } 
                break;
            case "OUT": 
                int val = GetValue(parts[1].Trim()); 
                OutputBuffer.Enqueue(val); 
                OnOutput?.Invoke(val); 
                break;
            default: 
                throw new Exception($"Unknown command: {opcode}");
        }
    }

    private int GetValue(string param) 
    { 
        if(int.TryParse(param, out int v)) return v; 
        if(Registers.ContainsKey(param)) return Registers[param]; 
        throw new Exception($"Invalid value: {param}"); 
    }
}   