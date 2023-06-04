struct NextLoad {
    public UInt32 RegisterIndex;
    public UInt32 Value;

    public NextLoad(UInt32 ui, UInt32 val) {
        RegisterIndex = ui;
        Value = val;
        
    }
}

enum Except : UInt32 {
    SysCall = 0x8
}

public class CPU {
    Hub hub;
    Instruction nextInstruction;
    NextLoad load;

    UInt32 pc;
    UInt32 nextPc;
    UInt32 currentPc;

    UInt32[] reg;
    UInt32[] outReg;

    UInt32 sr;
    UInt32 cause;
    UInt32 epc;
    UInt32 hi;
    UInt32 lo;

    int count = 0;

    public CPU() {
        this.pc = 0xbfc00000;
        this.nextPc = pc + 4;
        this.hub = new Hub();

        this.reg = new UInt32[32];
        Array.Fill<UInt32>(this.reg, 0xEEBBEEBB);
        this.reg[0] = 0x0;

        this.outReg = new UInt32[32];
        Array.Fill<UInt32>(this.reg, 0xBBBBEEEE);
        this.outReg[0] = 0x0;

        this.nextInstruction = new Instruction(0x0, 0);
        this.sr = 0x0;

        this.load = new NextLoad(0, 0);

        this.hi = 0xEE00EE00;
        this.lo = 0xEE00EE00;
    }

    public UInt32 getReg(UInt32 index) => reg[index];

    public void setReg(UInt32 index, UInt32 value) {
        if (index != 0) {
            hub.log.writeLog($"Writing register value {value} to index {index}");
        }

        outReg[index] = value;

        // Set register 0 to always be 0
        outReg[0] = 0x0;
    }

    public void runNextInstruction() {
        Instruction instruction = new Instruction(load32(pc), pc);

        pc = nextPc;
        nextPc += 4;

        setReg(load.RegisterIndex, load.Value);
        load = new NextLoad(0, 0);

        decodeAndExecute(instruction);

        reg = outReg;
    }

    public void decodeAndExecute(Instruction instruction) {
        Console.WriteLine($"Instruction: {instruction.primary():X} Whole opcode: {instruction.opcode:X}");

        hub.log.writeLog($"PC: {instruction.pc:X} instruction: {instruction.opcode:X} P: {instruction.primary():X} F: {instruction.funct():X} imm: {instruction.imm():X} imm_se: {instruction.imm_se():X}");

        hub.log.writeLogPC($"PC: {instruction.pc:X}");


        if (instruction.opcode == 0) {
            count++;
        } else {
            count = 0;
        }

        if (count >= 50) {
            throw new Exception("repeated empty opcodes");
        }

        switch (instruction.primary()) {
            case 0x00:
                Console.WriteLine($"funct: {instruction.funct():X}");
                switch (instruction.funct()) {
                    case 0x00:
                        op_sll(instruction); break;
                    case 0x02:
                        op_srl(instruction); break;
                    case 0x03:
                        op_sra(instruction); break;
                    case 0x08:
                        op_jr(instruction); break;
                    case 0x09:
                        op_jalr(instruction); break;
                    case 0x10:
                        op_mfhi(instruction); break;
                    case 0x12:
                        op_mflo(instruction); break;
                    case 0x1A:
                        op_div(instruction); break;
                    case 0x1B:
                        op_divu(instruction); break;
                    case 0x20:
                        op_add(instruction); break;
                    case 0x21:
                        op_addu(instruction); break;
                    case 0x23:
                        op_subu(instruction); break;
                    case 0x24:
                        op_and(instruction); break;
                    case 0x25:
                        op_or(instruction); break;
                    case 0x2A:
                        op_slt(instruction); break;
                    case 0x2B:
                        op_sltu(instruction); break;
                    default:
                        Console.WriteLine($"Unhandled funct instruction: {instruction.funct():X} {instruction.opcode:X}");
                        throw new Exception("Terminating due to unhandled instruction.");
                } break;
            case 0x01:
                op_bcondz(instruction); break;
            case 0x02:
                op_j(instruction); break;
            case 0x03:
                op_jal(instruction); break;
            case 0x04:
                op_beq(instruction); break;
            case 0x05:
                op_bne(instruction); break;
            case 0x06:
                op_blez(instruction); break;
            case 0x07:
                op_bgtz(instruction); break;
            case 0x08:
                op_addi(instruction); break;
            case 0x09:
                op_addiu(instruction); break;
            case 0x0A:
                op_slti(instruction); break;
            case 0x0B:
                op_sltiu(instruction); break;
            case 0x0C:
                op_andi(instruction); break;
            case 0x0D:
                op_ori(instruction); break;
            case 0x0F:
                op_lui(instruction); break;
            case 0x10:
                decodeAndExecuteCOP0(instruction); break;
            case 0x20:
                op_lb(instruction); break;
            case 0x23:
                op_lw(instruction); break;
            case 0x24:
                op_lbu(instruction); break;
            case 0x28:
                op_sb(instruction); break;
            case 0x29:
                op_sh(instruction); break;
            case 0x2B:
                op_sw(instruction); break;
            default:
                Console.WriteLine($"Unhandled primary instruction: {instruction.primary():X} {instruction.opcode:X}");
                throw new Exception("Terminating due to unhandled instruction.");
        } 
    }

    public void decodeAndExecuteCOP0(Instruction instruction) {
        Console.WriteLine($"COP0 Instruction: {instruction.rs():X} Whole opcode: {instruction.opcode:X}");

        switch (instruction.rs()) {
            case 0x00:
                op_mfc0(instruction); break;
            case 0x04:
                op_mtc0(instruction); break;
            default:
                throw new Exception($"Unhandled COP0 instruction: {instruction.rs():X}");
        }
    }

    private bool checkOverflow(uint a, uint b, uint r) => ((r ^ a) & (r ^ b) & 0x8000_0000) != 0;

    private bool checkUnderflow(uint a, uint b, uint r) => ((r ^ a) & (a ^ b) & 0x8000_0000) != 0;

    private byte load8(UInt32 addr) {
        return hub.load8(addr);
    }

    private UInt32 load32(UInt32 addr) {
        return hub.load32(addr);
    }

    private void store8(UInt32 addr, byte val) {
        hub.store8(addr, val);
    }

    private void store16(UInt32 addr, UInt16 val) {
        hub.store16(addr, val);
    }

    private void store32(UInt32 addr, UInt32 val) {
        hub.store32(addr, val);
    }

    private void branch(UInt32 offset) {
        hub.log.writeLog($"Branching to addr: {pc+offset:X} offset signed: {(Int32)offset} unsigned: {offset} hex: {offset:X}");
        nextPc = pc + (offset << 2);
    }

    private void triggerException() {

    }

    // opcodes
    private void op_mfhi(Instruction instruction) {
        UInt32 rd = instruction.rd();
        setReg(rd, hi);
    }

    private void op_srl(Instruction instruction) {
        UInt32 shamt = instruction.shamt();
        UInt32 rt = instruction.rt();
        UInt32 rd = instruction.rd();

        UInt32 val = getReg(rt) >> (int)shamt;

        setReg(rd, val);
    }

    private void op_mflo(Instruction instruction) {
        UInt32 rd = instruction.rd();
        setReg(rd, lo);
    }

    private void op_divu(Instruction instruction) {
        UInt32 rs = instruction.rs();
        UInt32 rt = instruction.rt();

        UInt32 num = getReg(rs);
        UInt32 denom = getReg(rt);

        if (denom == 0) {
            hi = num;
            lo = 0xFFFFFFFF;
        } else {
            hi = num % denom;
            lo = num / denom;
        }
    }

    private void op_div(Instruction instruction) {
        UInt32 rs = instruction.rs();
        UInt32 rt = instruction.rt();

        Int32 num = (Int32)getReg(rs);
        Int32 denom = (Int32)getReg(rt);

        if (denom == 0) {
            hi = (UInt32)num;

            if (num >= 0) {
                lo = 0xFFFFFFFF;
            } else {
                lo = 0x1;
            }
        } else if (((UInt32)num == 0x80000000) && denom == -1) {
            hi = 0x0;
            lo = 0x80000000;
        } else {
            hi = (UInt32)(num % denom);
            lo = (UInt32)(num / denom);
        }
    }

    private void op_sra(Instruction instruction) {
        UInt32 i = instruction.shamt();
        UInt32 rt = instruction.rt();
        UInt32 rd = instruction.rd();

        Int32 val = (Int32)getReg(rt) >> (Int32)i;

        setReg(rd, (UInt32)val);
    }

    private void op_subu(Instruction instruction) {
        UInt32 rd = instruction.rd();
        UInt32 rs = instruction.rs();
        UInt32 rt = instruction.rt();

        UInt32 val = getReg(rs) - getReg(rt);

        setReg(rd, val);
    }
    
    private void op_bcondz(Instruction instruction) {
        UInt32 i = instruction.imm_se();
        UInt32 rs = instruction.rs();

        bool bgez = ((instruction.opcode >> 16) & 0x1) == 1;
        bool link = ((instruction.opcode >> 20) & 0x1) == 1;

        Int32 val = (Int32)getReg(rs);

        bool takeBranch = (val < 0 && !bgez) || (val >= 0 && bgez);

        hub.log.writeLog($"value: {val} takebranch: {takeBranch} bgez: {bgez} link: {link}");

        if (link) {
            setReg(31, nextPc);
        }
        if (takeBranch) {
            branch(i);
        }
    }

    private void op_sltiu(Instruction instruction) {
        UInt32 i = instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        UInt32 val = 0;

        if (getReg(rs) < i) {
            val = 1;
        } 

        setReg(rt, val);
    }

    private void op_slti(Instruction instruction) {
        Int32 i = (Int32)instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        UInt32 val = 0;

        if ((Int32)getReg(rs) < i) {
            val = 1;
        } 

        setReg(rt, val);
    }

    private void op_jalr(Instruction instruction) {
        UInt32 rd = instruction.rd();
        UInt32 rs = instruction.rs();

        UInt32 ra = nextPc;

        setReg(rd, ra);

        nextPc = getReg(rs);

        hub.log.writeLog($"jalr rs {rs}: {getReg(rs):X}");
        hub.log.writeLog($"pc set to {pc:X}");
    }

    private void op_blez(Instruction instruction) {
        UInt32 i = instruction.imm_se();
        UInt32 rs = instruction.rs();

        hub.log.writeLog($"blez rs {rs}: {(Int32)getReg(rs):X}");

        if ((Int32)getReg(rs) <= 0) {
            branch(i);
        }
    }

    private void op_bgtz(Instruction instruction) {
        UInt32 i = instruction.imm_se();
        UInt32 rs = instruction.rs();

        hub.log.writeLog($"bgtz rs {rs}: {(Int32)getReg(rs):X}");

        if ((Int32)getReg(rs) > 0) {
            branch(i);
        }
    }

    private void op_and(Instruction instruction) {
        UInt32 rd = instruction.rd();
        UInt32 rs = instruction.rs();
        UInt32 rt = instruction.rt();

        UInt32 val = getReg(rs) & getReg(rt);

        setReg(rd, val);
    }

    private void op_beq(Instruction instruction) {
        UInt32 i = instruction.imm_se();
        UInt32 rs = instruction.rs();
        UInt32 rt = instruction.rt();

        hub.log.writeLog($"beq: reg {rs} {getReg(rs):X} == reg {rt} {getReg(rt):X}");

        if (getReg(rs) == getReg(rt)) {
            branch(i);
        }
    }

    private void op_jal(Instruction instruction) {
        UInt32 ra = nextPc;
        Console.WriteLine($"jal PC: {nextPc:X}");
        setReg(31, ra);
        Console.WriteLine($"reg 31: {getReg(31):X}");
        op_j(instruction);
    }

    private void op_jr(Instruction instruction) {
        UInt32 rs = instruction.rs();
        Console.WriteLine(rs);
        Console.WriteLine($"jr setting pc to: {getReg(rs):X}");
        nextPc = getReg(rs);
        hub.log.writeLog($"pc set to {pc:X}");
    }

    private void op_add(Instruction instruction) {
        UInt32 rd = instruction.rd();
        UInt32 rs = instruction.rs();
        UInt32 rt = instruction.rt();

        UInt32 val = getReg(rs) + getReg(rt);

        if (checkOverflow(getReg(rs), getReg(rt), val)) {
            hub.log.writeLog("add overflow");
            return;
        }

        setReg(rd, val);
    }

    private void op_addu(Instruction instruction) {
        UInt32 rd = instruction.rd();
        UInt32 rs = instruction.rs();
        UInt32 rt = instruction.rt();

        UInt32 val = getReg(rs) + getReg(rt);

        setReg(rd, val);
    }

    private void op_lui(Instruction instruction) {
        UInt32 i = instruction.imm();
        UInt32 rt = instruction.rt();

        UInt32 val = i << 16;

        setReg(rt, val);
    }

    private void op_andi(Instruction instruction) {
        UInt32 i = instruction.imm();
        UInt32 rs = instruction.rs();
        UInt32 rt = instruction.rt();

        UInt32 val = getReg(rs) & i;

        setReg(rt, val);
    }

    private void op_ori(Instruction instruction) {
        UInt32 i = instruction.imm();
        UInt32 rs = instruction.rs();
        UInt32 rt = instruction.rt();

        UInt32 val = getReg(rs) | i;

        setReg(rt, val);
    }

    private void op_lb(Instruction instruction) {
        if ((sr & 0x10000) != 0) {
            hub.log.writeLog("Ignoring lb, cache is isolated");
            return;
        }

        UInt32 i = instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        UInt32 addr = getReg(rs) + i;

        hub.log.writeLog($"lb {load8(addr):X}");

        UInt32 val = (UInt32)(sbyte)load8(addr);

        hub.log.writeLog($"val after casting lb: {val:X}");

        load = new NextLoad(rt, val);
    }

    private void op_lbu(Instruction instruction) {
        if ((sr & 0x10000) != 0) {
            hub.log.writeLog("Ignoring lbu, cache is isolated");
            return;
        }

        UInt32 i = instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        UInt32 addr = getReg(rs) + i;

        hub.log.writeLog($"lb {load8(addr):X}");

        UInt32 val = load8(addr);

        hub.log.writeLog($"val after casting lbu: {val:X}");

        load = new NextLoad(rt, val);
    }

    private void op_sb(Instruction instruction) {
        if ((sr & 0x10000) != 0) {
            hub.log.writeLog("Ignoring sb, cache is isolated");
            return;
        }

        UInt32 i = instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        UInt32 addr = getReg(rs) + i;
        UInt32 val = getReg(rt);

        hub.log.writeLog($"sb value: {val:X} sb cast value: {(byte)val:X}");

        store8(addr, (byte)val);
    }

    private void op_sw(Instruction instruction) {
        if ((sr & 0x10000) != 0) {
            // cache is isolated, ignore write
            hub.log.writeLog("Ignoring sw, cache is isolated");
            return;
        }

        UInt32 i = instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        UInt32 addr = getReg(rs) + i;
        UInt32 val = getReg(rt);

        store32(addr, val);
    }

    private void op_sll(Instruction instruction) {
        UInt32 shamt = instruction.shamt();
        UInt32 rt = instruction.rt();
        UInt32 rd = instruction.rd();

        UInt32 val = getReg(rt) << (int)shamt;

        setReg(rd, val);
    }

    private void op_addi(Instruction instruction) {
        UInt32 i = instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        Console.WriteLine($"addi rs {rs}: {(Int32)getReg(rs):X} i: {i:X}");

        UInt32 val = getReg(rs) + i;

        if (checkOverflow(getReg(rs), i, val)) {
            throw new Exception("addi overflow");
        }

        setReg(rt, val);
    }

    private void op_addiu(Instruction instruction) {
        UInt32 i = instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        UInt32 val = getReg(rs) + i;

        setReg(rt, val);
    }

    private void op_j(Instruction instruction) {
        UInt32 pcTemp = pc;
        UInt32 addr = instruction.tar();

        pcTemp = (pcTemp & 0xF0000000) | (addr << 2);

        nextPc = pcTemp;
    }

    private void op_or(Instruction instruction) {
        UInt32 rd = instruction.rd();
        UInt32 rs = instruction.rs();
        UInt32 rt = instruction.rt();
        
        UInt32 val = getReg(rs) | getReg(rt);

        setReg(rd, val);
    }

    private void op_bne(Instruction instruction) {
        UInt32 i = instruction.imm_se();
        UInt32 rs = instruction.rs();
        UInt32 rt = instruction.rt();

        hub.log.writeLog($"bne:  reg {rs}: {getReg(rs):X} = reg {rt}: {getReg(rt):X}");

        if (getReg(rs) != getReg(rt)) {
            branch(i);
        }
    }

    private void op_lw(Instruction instruction) {
        if ((sr & 0x10000) != 0) {
            hub.log.writeLog("Cache isolated, ignoring lw");
            return;
        }

        UInt32 i = instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        UInt32 addr = getReg(rs) + i;

        UInt32 val = load32(addr);

        load = new NextLoad(rt, val);
    }

    private void op_slt(Instruction instruction) {
        UInt32 rd = instruction.rd();
        UInt32 rs = instruction.rs();
        UInt32 rt = instruction.rt();

        UInt32 val = 0;
        if ((Int32)getReg(rs) < (Int32)getReg(rt)) {
            val = 1;
        }

        setReg(rd, val);
    }

    private void op_sltu(Instruction instruction) {
        UInt32 rd = instruction.rd();
        UInt32 rs = instruction.rs();
        UInt32 rt = instruction.rt();

        UInt32 val = 0;
        if (getReg(rs) < getReg(rt)) {
            val = 1;
        }

        setReg(rd, val);
    }

    private void op_sh(Instruction instruction) {
        if ((sr & 0x10000) != 0) {
            hub.log.writeLog("Ignoring sh, cache is isolated");
            return;
        }

        UInt32 i = instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        UInt32 addr = getReg(rs) + i;
        UInt16 val = (UInt16)(getReg(rt) & 0xFFFF);

        store16(addr, val);
    }

    // COP0 opcodes
    private void op_mtc0(Instruction instruction) {
        UInt32 cpuReg = instruction.rt();
        UInt32 copReg = instruction.rd();

        UInt32 val = getReg(cpuReg);

        hub.log.writeLog($"mtc copReg: {copReg} value: {val:X}");

        switch (copReg) {
            case 3 or 5 or 6 or 7 or 9 or 11:
                if (val != 0) {
                    throw new Exception($"Unhandled write to COP0 reg: {copReg:X} value: {val:X}");
                } break;
            case 12:
                sr = val; break;
            case 13:
                if (val != 0) {
                    throw new Exception("Unhandled write to COP0 CAUSE register");
                } break;
            default:
                throw new Exception($"Unhandled COP0 Register: {copReg}");
        }
    }

    private void op_mfc0(Instruction instruction) {
        UInt32 cpuReg = instruction.rt();
        UInt32 copReg = instruction.rd();

        UInt32 val = 0;

        hub.log.writeLog($"mfc copReg: {copReg}");

        switch (copReg) {
            case 12:
                val = sr; break;
            case 13:
                val = cause; break;
            case 14:
                val = epc; break;
            default:
                throw new Exception($"Unhandled read from COP0 Register: {copReg}");
        }

        hub.log.writeLog($"mfc value: {val}");

        load = new NextLoad(cpuReg, val);
    }
}

public class Instruction {
    public UInt32 opcode;
    public UInt32 pc;

    public Instruction(UInt32 opcode, UInt32 pc) {
        this.opcode = opcode;
        this.pc = pc;
    } 

    public UInt32 primary() => opcode >> 26;

    public UInt32 rs() => (opcode >> 21) & 0x1F;

    public UInt32 rt() => (opcode >> 16) & 0x1F;

    public UInt32 rd() => (opcode >> 11) & 0x1F;

    public UInt32 shamt() => (opcode >> 6) & 0x1F;

    public UInt32 funct() => opcode & 0x3F;

    public UInt32 imm() => opcode & 0xFFFF;

    public UInt32 tar() => opcode & 0x3FFFFFF;

    public UInt32 imm_se() {
        UInt32 val = (UInt32)(Int16)opcode;

        return val;
    }
}