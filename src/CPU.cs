struct NextLoad {
    public UInt32 RegisterIndex;
    public UInt32 Value;

    public NextLoad(UInt32 ui, UInt32 val) {
        RegisterIndex = ui;
        Value = val;
    }
}

public class CPU {
    UInt32 pc;
    Hub hub;
    UInt32[] reg;
    UInt32[] outReg;
    Instruction nextInstruction;
    UInt32 sr;
    NextLoad load;
    UInt32 hi;
    UInt32 lo;
    int count = 0;

    public CPU() {
        this.pc = 0xbfc00000;
        this.hub = new Hub();

        this.reg = new UInt32[32];
        Array.Fill<UInt32>(this.reg, 0xEEBBEEBB);
        this.reg[0] = 0x0;

        this.outReg = new UInt32[32];
        Array.Fill<UInt32>(this.reg, 0xBBBBEEEE);
        this.outReg[0] = 0x0;

        this.nextInstruction = new Instruction(0x0);
        this.sr = 0x0;

        this.load = new NextLoad(0, 0);

        this.hi = 0xEE00EE00;
        this.lo = 0xEE00EE00;
    }

    public UInt32 getReg(UInt32 index) => reg[index];

    public void setReg(UInt32 index, UInt32 value) {
        Console.WriteLine($"Writing register value {value} to index {index}");

        outReg[index] = value;

        // Set register 0 to always be 0
        outReg[0] = 0x0;
    }

    public void runNextInstruction() {
        setReg(load.RegisterIndex, load.Value);

        load = new NextLoad(0, 0);

        Instruction instruction = nextInstruction;

        nextInstruction = new Instruction(hub.load32(pc));

        pc += 4;

        decodeAndExecute(instruction);

        reg = outReg;
    }

    public void decodeAndExecute(Instruction instruction) {
        Console.WriteLine($"Instruction: {instruction.primary():X} Whole opcode: {instruction.opcode:X}");

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
                    case 0x12:
                        op_mflo(instruction); break;
                    case 0x1A:
                        op_div(instruction); break;
                    case 0x1B:
                        op_divu(instruction); break;
                    case 0x25:
                        op_or(instruction); break;
                    case 0x2B:
                        op_sltu(instruction); break;
                    case 0x21:
                        op_addu(instruction); break;
                    case 0x23:
                        op_subu(instruction); break;
                    case 0x24:
                        op_and(instruction); break;
                    case 0x20:
                        op_add(instruction); break;
                    default:
                        Console.WriteLine($"Unhandled funct instruction: {instruction.funct():X} {instruction.opcode:X}");
                        throw new Exception("Terminating due to unhandled instruction.");
                } break;
            case 0x01:
                op_bcondz(instruction); break;
            case 0x03:
                op_jal(instruction); break;
            case 0x04:
                op_beq(instruction); break;
            case 0x06:
                op_blez(instruction); break;
            case 0x07:
                op_bgtz(instruction); break;
            case 0x09:
                op_addiu(instruction); break;
            case 0x0A:
                op_slti(instruction); break;
            case 0x0B:
                op_sltiu(instruction); break;
            case 0x0C:
                op_andi(instruction); break;
            case 0x0F:
                op_lui(instruction); break;
            case 0x0D:
                op_ori(instruction); break;
            case 0x2B:
                op_sw(instruction); break;
            case 0x02:
                op_j(instruction); break;
            case 0x10:
                decodeAndExecuteCOP0(instruction); break;
            case 0x05:
                op_bne(instruction); break;
            case 0x08:
                op_addi(instruction); break;
            case 0x20:
                op_lb(instruction); break;
            case 0x23:
                op_lw(instruction); break;
            case 0x28:
                op_sb(instruction); break;
            case 0x29:
                op_sh(instruction); break;
            case 0x24:
                op_lbu(instruction); break;
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

    private byte load8(UInt32 addr) {
        return hub.load8(addr);
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
        offset = offset << 2;
        pc = pc + offset;
        pc -= 4;
    }

    // opcodes
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

            return;
        }

        if (((UInt32)num == 0x80000000) && denom == -1) {
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

        bool bgez = ((instruction.opcode >> 16) & 1) == 1;
        bool link = ((instruction.opcode >> 20) & 1) == 1;

        Int32 val = (Int32)getReg(rs);

        if ((val < 0 && !bgez) || (val >= 0 && bgez)) {
            if (link) {
                setReg(31, pc);
            }

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

        UInt32 ra = pc;

        setReg(rd, ra);

        pc = getReg(rs);
    }

    private void op_blez(Instruction instruction) {
        UInt32 i = instruction.imm_se();
        UInt32 rs = instruction.rs();

        if (getReg(rs) <= 0) {
            branch(i);
        }
    }

    private void op_bgtz(Instruction instruction) {
        UInt32 i = instruction.imm_se();
        UInt32 rs = instruction.rs();

        if (getReg(rs) > 0) {
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

        if (getReg(rs) == getReg(rt)) {
            branch(i);
        }
    }

    private void op_jal(Instruction instruction) {
        UInt32 ra = pc;
        Console.WriteLine($"jal PC: {pc:X}");
        setReg(31, ra);
        Console.WriteLine($"reg 31: {getReg(31):X}");
        op_j(instruction);
    }

    private void op_jr(Instruction instruction) {
        UInt32 rs = instruction.rs();
        Console.WriteLine(rs);
        Console.WriteLine($"jr setting pc to: {getReg(rs):X}");
        pc = getReg(rs);
    }

    private void op_add(Instruction instruction) {
        UInt32 rd = instruction.rd();
        UInt32 rs = instruction.rs();
        UInt32 rt = instruction.rt();

        UInt32 val = checked(getReg(rs) + getReg(rt));

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
        UInt32 i = instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        UInt32 addr = getReg(rs) + i;

        UInt32 val = (UInt32)(Int32)load8(addr);

        load = new NextLoad(rt, val);
    }

    private void op_lbu(Instruction instruction) {
        UInt32 i = instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        UInt32 addr = getReg(rs) + i;

        UInt32 val = load8(addr);

        load = new NextLoad(rt, val);
    }

    private void op_sb(Instruction instruction) {
        if ((sr & 0x10000) != 0) {
            Console.WriteLine("Ignoring store, cache is isolated");
            return;
        }

        UInt32 i = instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        UInt32 addr = getReg(rs) + i;
        UInt32 val = getReg(rt);

        store8(addr, (byte)(val & 0xFF));
    }

    private void op_sw(Instruction instruction) {
        if ((sr & 0x10000) != 0) {
            // cache is isolated, ignore write
            Console.WriteLine("Ignoring store, cache is isolated");
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
        Int32 i = (Int32)instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        Int32 val = (Int32)getReg(rs);

        val = checked(val + i);

        setReg(rt, (UInt32)val);
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

        pcTemp = (pcTemp & 0xF0000000) + (addr * 4);

        pc = pcTemp;
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

        if (getReg(rs) != getReg(rt)) {
            branch(i);
        }
    }

    private void op_lw(Instruction instruction) {
        if ((sr & 0x10000) != 0) {
            Console.WriteLine("Cache isolated, ignoring load");
            return;
        }

        UInt32 i = instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        UInt32 addr = getReg(rs) + i;

        UInt32 val = hub.load32(addr);

        load = new NextLoad(rt, val);
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
            Console.WriteLine("Ignoring store, cache is isolated");
            return;
        }

        UInt32 i = instruction.imm_se();
        UInt32 rt = instruction.rt();
        UInt32 rs = instruction.rs();

        UInt32 addr = getReg(rs) + i;
        UInt32 val = getReg(rt);

        store16(addr, (UInt16)val);
    }

    // COP0 opcodes
    private void op_mtc0(Instruction instruction) {
        UInt32 cpuReg = instruction.rt();
        UInt32 copReg = instruction.rd();

        UInt32 val = getReg(cpuReg);

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

        switch (copReg) {
            case 12:
                val = sr; break;
            default:
                throw new Exception($"Unhandled read from COP0 Register: {copReg}");
        }

        load = new NextLoad(cpuReg, val);
    }
}

public class Instruction {
    public UInt32 opcode;

    public Instruction(UInt32 opcode) => this.opcode = opcode;

    public UInt32 primary() => opcode >> 26;

    public UInt32 rs() => (opcode >> 21) & 0x1F;

    public UInt32 rt() => (opcode >> 16) & 0x1F;

    public UInt32 rd() => (opcode >> 11) & 0x1F;

    public UInt32 shamt() => (opcode >> 6) & 0x1F;

    public UInt32 funct() => opcode & 0x3F;

    public UInt32 imm() => opcode & 0xFFFF;

    public UInt32 tar() => opcode & 0x3FFFFFF;

    public UInt32 imm_se() {
        Int16 val = (Int16)(opcode & 0xFFFF);

        return (UInt32)(Int32)val;
    }
}