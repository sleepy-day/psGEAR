struct NextLoad {
    public UInt32 RegisterIndex;
    public UInt32 Value;

    public NextLoad(UInt32 ui, UInt32 val) {
        RegisterIndex = ui;
        Value = val;
        
    }
}

enum Except : UInt32 {
    LoadAddressError = 0x4,
    StoreAddressError = 0x5,
    SysCall = 0x8,
    Overflow = 0xC,
    Break = 0x9,
    CoprocessorError = 0xB,
    IllegalInstruction = 0xA,
}

public class CPU {
    Hub hub;
    Instruction nextInstruction;
    NextLoad load;

    UInt32 pc;
    UInt32 nextPc;
    UInt32 currentPc;

    bool branchTaken;
    bool delaySlot;

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

        this.branchTaken = false;
        this.delaySlot = false;
    }

    public UInt32 getReg(UInt32 index) => reg[index];

    public void setReg(UInt32 index, UInt32 value) {
        if (index != 0) {
            hub.log.writeLog($"Writing register value {value} to index {index}");
        }

        this.outReg[index] = value;

        // Set register 0 to always be 0
        this.outReg[0] = 0x0;
    }

    public void runNextInstruction() {
        Instruction instruction = new Instruction(load32(pc), pc);

        this.pc = this.nextPc;
        this.nextPc += 4;

        if (this.pc % 4 != 0) {
            triggerException(Except.LoadAddressError);
        }

        this.delaySlot = this.branchTaken;
        this.branchTaken = false;

        setReg(this.load.RegisterIndex, this.load.Value);
        this.load = new NextLoad(0, 0);

        decodeAndExecute(instruction);

        this.reg = this.outReg;
    }

    public void decodeAndExecute(Instruction instruction) {
        Console.WriteLine($"Instruction: {instruction.primary():X} Whole opcode: {instruction.opcode:X}");

        hub.log.writeLog($"PC: {instruction.pc:X} instruction: {instruction.opcode:X} P: {instruction.primary():X} F: {instruction.funct():X} imm: {instruction.imm():X} imm_se: {instruction.imm_se():X}");

        hub.log.writeLogPC($"PC: {instruction.pc:X}");

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
                    case 0x04:
                        op_sllv(instruction); break;
                    case 0x06:
                        op_srlv(instruction); break;
                    case 0x07:
                        op_srav(instruction); break;
                    case 0x08:
                        op_jr(instruction); break;
                    case 0x09:
                        op_jalr(instruction); break;
                    case 0x0C:
                        op_syscall(instruction); break;
                    case 0x0D:
                        op_break(instruction); break;
                    case 0x10:
                        op_mfhi(instruction); break;
                    case 0x11:
                        op_mthi(instruction); break;
                    case 0x12:
                        op_mflo(instruction); break;
                    case 0x13:
                        op_mtlo(instruction); break;
                    case 0x18:
                        op_mult(instruction); break;
                    case 0x19:
                        op_multu(instruction); break;
                    case 0x1A:
                        op_div(instruction); break;
                    case 0x1B:
                        op_divu(instruction); break;
                    case 0x20:
                        op_add(instruction); break;
                    case 0x21:
                        op_addu(instruction); break;
                    case 0x22:
                        op_sub(instruction); break;
                    case 0x23:
                        op_subu(instruction); break;
                    case 0x24:
                        op_and(instruction); break;
                    case 0x25:
                        op_or(instruction); break;
                    case 0x26:
                        op_xor(instruction); break;
                    case 0x27:
                        op_nor(instruction); break;
                    case 0x2A:
                        op_slt(instruction); break;
                    case 0x2B:
                        op_sltu(instruction); break;
                    default:
                        op_illegal(instruction); break;
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
            case 0x0E:
                op_xori(instruction); break;
            case 0x0F:
                op_lui(instruction); break;
            case 0x10:
                decodeAndExecuteCOP0(instruction); break;
            case 0x11 or 0x13:
                op_copInv(instruction); break;
            case 0x12:
                op_cop2(instruction); break;
            case 0x20:
                op_lb(instruction); break;
            case 0x21:
                op_lh(instruction); break;
            case 0x22:
                op_lwl(instruction); break;
            case 0x23:
                op_lw(instruction); break;
            case 0x24:
                op_lbu(instruction); break;
            case 0x25:
                op_lhu(instruction); break;
            case 0x26:
                op_lhu(instruction); break;
            case 0x27:
                op_nor(instruction); break;
            case 0x28:
                op_sb(instruction); break;
            case 0x29:
                op_sh(instruction); break;
            case 0x2A:
                op_swl(instruction); break;
            case 0x2B:
                op_sw(instruction); break;
            case 0x2E:
                op_swr(instruction); break;
            case 0x30 or 0x31 or 0x33:
                op_lwcInv(instruction); break;
            case 0x32:
                op_lwc2(instruction); break;
            case 0x38 or 0x39 or 0x3B:
                op_swcInv(instruction); break;
            case 0x3A:
                op_swc2(instruction); break;
            default:
                op_illegal(instruction); break;
        } 
    }

    public void decodeAndExecuteCOP0(Instruction instruction) {
        Console.WriteLine($"COP0 Instruction: {instruction.rs():X} Whole opcode: {instruction.opcode:X}");

        switch (instruction.rs()) {
            case 0x00:
                op_mfc0(instruction); break;
            case 0x04:
                op_mtc0(instruction); break;
            case 0x10:
                op_rfe(instruction); break;
            default:
                throw new Exception($"Unhandled COP0 instruction: {instruction.rs():X}");
        }
    }

    // checking bitwise overflow and underflows
    private bool checkOverflow(uint a, uint b, uint r) => ((r ^ a) & (r ^ b) & 0x8000_0000) != 0;

    private bool checkUnderflow(uint a, uint b, uint r) => ((r ^ a) & (a ^ b) & 0x8000_0000) != 0;

    // load instructions
    private byte load8(UInt32 addr) => hub.load8(addr);

    private UInt16 load16(UInt32 addr) => hub.load16(addr);
    
    private UInt32 load32(UInt32 addr) =>hub.load32(addr);

    // store instructions
    private void store8(UInt32 addr, byte val) => hub.store8(addr, val);

    private void store16(UInt32 addr, UInt16 val) => hub.store16(addr, val);

    private void store32(UInt32 addr, UInt32 val) => hub.store32(addr, val);

    // branch instruction used in all branch if/if not instructions
    private void branch(UInt32 offset) {
        this.nextPc = this.pc + (offset << 2);
        this.branchTaken = true;
    }

    // general purpose exception function
    private void triggerException(Except excep) {
        UInt32 handlerAddr = 0x80000080;

        if (((sr >> 22) & 0x1) == 1) 
            handlerAddr = 0xbfc00180;

        UInt32 mode = this.sr & 0x3f;
        this.sr &= unchecked((UInt32)~0x3f);
        this.sr |= (mode << 2) & 0x3f;

        this.cause = (UInt32)excep << 2;

        this.epc = this.pc;

        if (delaySlot) {
            this.epc -= 4;
            this.cause |= (UInt32)1 << 31;
        }

        this.pc = handlerAddr;
        this.nextPc = this.pc + 4;
    }

    // opcodes
    private void op_illegal(Instruction ins) => triggerException(Except.IllegalInstruction);

    private void op_lwl(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 imm = ins.imm_se();

        UInt32 addr = rs + imm;
        UInt32 prev = outReg[ins.rt()];

        UInt32 alignedAddr = addr & ~0x3U;
        UInt32 offset = addr & 0x3;

        UInt32 word = load32(alignedAddr);

        UInt32 val = 0;
        switch (offset) {
            case 0:
                val = (prev & 0x00FFFFFF) | (word << 24); break;
            case 1:
                val = (prev & 0x0000FFFF) | (word << 16); break;
            case 2:
                val = (prev & 0x000000FF) | (word << 8); break;
            case 3:
                val = word; break;
        }

        setReg(ins.rt(), val);
    }

    private void op_lwr(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 imm = ins.imm_se();

        UInt32 addr = rs + imm;
        UInt32 prev = outReg[ins.rt()];

        UInt32 alignedAddr = addr & ~0x3U;
        UInt32 offset = addr & 0x3;

        UInt32 word = load32(alignedAddr);

        UInt32 val = 0;
        switch (offset) {
            case 0:
                val = word; break;
            case 1:
                val = (prev & 0xFF000000) | (word >> 8); break;
            case 2:
                val = (prev & 0xFFFF0000) | (word >> 16); break;
            case 3:
                val = (prev & 0xFFFFFF00) | (word >> 24); break;
        }

        setReg(ins.rt(), val);
    }

    private void op_swl(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 imm = ins.imm_se();

        UInt32 addr = rs + imm;

        UInt32 alignedAddr = addr & ~0x3U;
        UInt32 offset = addr & 0x3;

        UInt32 word = load32(alignedAddr);
        UInt32 regVal = getReg(ins.rt());

        UInt32 val = 0;
        switch (offset) {
            case 0:
                val = (word & 0xFFFFFF00) | (regVal >> 24); break;
            case 1:
                val = (word & 0xFFFF0000) | (regVal >> 16); break;
            case 2:
                val = (word & 0xFF000000) | (regVal >> 8); break;
            case 3:
                val = word; break;
        }

        store32(alignedAddr, val);
    }

    private void op_swr(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 imm = ins.imm_se();

        UInt32 addr = rs + imm;

        UInt32 alignedAddr = addr & ~0x3U;
        UInt32 offset = addr & 0x3;

        UInt32 word = load32(alignedAddr);
        UInt32 regVal = getReg(ins.rt());

        UInt32 val = 0;
        switch (offset) {
            case 0:
                val = word; break;
            case 1:
                val = (word & 0x000000FF) | (regVal << 8); break;
            case 2:
                val = (word & 0x0000FFFF) | (regVal << 16); break;
            case 3:
                val = (word & 0x00FFFFFF) | (regVal << 24); break;
        }

        store32(alignedAddr, val);
    }

    // only supported by cop2, otherwise they all trigger the same exception
    private void op_lwcInv(Instruction ins) => triggerException(Except.CoprocessorError);

    // only supported by cop2
    private void op_swcInv(Instruction ins) => triggerException(Except.CoprocessorError);

    // cop 1/3 aren't used on the PSX and will trigger the same exception if accessed
    private void op_copInv(Instruction ins) => triggerException(Except.CoprocessorError);
    
    private void op_lwc2(Instruction ins) {
        throw new Exception($"Unhandled GTE LWC: {ins.opcode:X}");
    }

    private void op_swc2(Instruction ins) {
        throw new Exception($"Unhandled GTE SWC: {ins.opcode:X}");
    }

    private void op_cop2(Instruction ins) => throw new Exception($"Unhandled cop2 instruction: {ins.opcode:X}");

    private void op_break(Instruction ins) => triggerException(Except.Break);

    private void op_xor(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 rt = getReg(ins.rt());

        UInt32 val = rs ^ rt;

        setReg(ins.rd(), val);
    }

    private void op_xori(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 imm = ins.imm();

        UInt32 val = rs ^ imm;

        setReg(ins.rt(), val);
    }

    private void op_mult(Instruction ins) {
        Int64 rs = (Int64)getReg(ins.rs());
        Int64 rt = (Int64)getReg(ins.rt());

        UInt64 val = (UInt64)(rs * rt);

        this.hi = (UInt32)(val >> 32);
        this.lo = (UInt32)(val);
    }

    private void op_multu(Instruction ins) {
        UInt64 rs = getReg(ins.rs());
        UInt64 rt = getReg(ins.rt());

        UInt64 val = rs * rt;

        this.hi = (UInt32)(val >> 32);
        this.lo = (UInt32)(val);
    }

    private void op_srlv(Instruction ins) {
        UInt32 rt = getReg(ins.rt());
        Int32 rs = (Int32)(getReg(ins.rs()) & 0x1F);

        UInt32 val = rt >> rs;

        setReg(ins.rd(), val);
    }

    private void op_srav(Instruction ins) {
        Int32 rt = (Int32)getReg(ins.rt());
        Int32 rs = (Int32)(getReg(ins.rs()) & 0x1F);

        UInt32 val = (UInt32)(rt >> rs);

        setReg(ins.rd(), val);
    }

    private void op_nor(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 rt = getReg(ins.rt());

        UInt32 val = ~(rs | rt);

        setReg(ins.rd(), val);
    }

    private void op_sllv(Instruction ins) {
        Int32 rs = (Int32)(getReg(ins.rs() & 0x1F));
        UInt32 rt = getReg(ins.rt());

        UInt32 val = rt << rs;

        setReg(ins.rd(), val);
    }

    private void op_rfe(Instruction ins) {
        if ((ins.opcode & 0x3F) != 0x10) {
            throw new Exception($"Invalid rfe instruction: {ins.opcode:X}" );
        }

        UInt32 mode = this.sr & 0x3F;
        this.sr &= unchecked((UInt32)~0x3F);
        this.sr |= mode >> 2;
    }

    // instructions relating to hi/lo need to be fixed to account for delay in calc
    private void op_mthi(Instruction ins) => this.hi = getReg(ins.rs());

    private void op_mtlo(Instruction ins) => this.lo = getReg(ins.rs());
    
    private void op_mfhi(Instruction instruction) {
        UInt32 rd = instruction.rd();
        setReg(rd, hi);
    }

    private void op_mflo(Instruction instruction) {
        UInt32 rd = instruction.rd();
        setReg(rd, lo);
    }

    private void op_srl(Instruction ins) {
        Int32 shamt = (Int32)ins.shamt();
        UInt32 rt = getReg(ins.rt());

        UInt32 val = rt >> shamt;

        setReg(ins.rd(), val);
    }

    private void op_syscall(Instruction instruction) => triggerException(Except.SysCall);

    private void op_divu(Instruction ins) {
        UInt32 num = getReg(ins.rs());
        UInt32 denom = getReg(ins.rt());

        if (denom == 0) {
            this.hi = num;
            this.lo = 0xFFFFFFFF;
        } else {
            this.hi = num % denom;
            this.lo = num / denom;
        }
    }

    private void op_div(Instruction ins) {
        Int32 num = (Int32)getReg(ins.rs());
        Int32 denom = (Int32)getReg(ins.rt());

        if (denom == 0) {
            this.hi = (UInt32)num;

            if (num >= 0) {
                this.lo = 0xFFFFFFFF;
            } else {
                this.lo = 0x1;
            }
        } else if (((UInt32)num == 0x80000000) && denom == -1) {
            this.hi = 0x0;
            this.lo = 0x80000000;
        } else {
            this.hi = (UInt32)(num % denom);
            this.lo = (UInt32)(num / denom);
        }
    }

    private void op_sra(Instruction ins) {
        Int32 shamt = (Int32)ins.shamt();
        Int32 rt = (Int32)getReg(ins.rt());

        Int32 val = rt >> shamt;

        setReg(ins.rd(), (UInt32)val);
    }

    private void op_sub(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 rt = getReg(ins.rt());
        UInt32 result = rs - rt;

        if (checkUnderflow(rs, rt, result)) {
            triggerException(Except.Overflow);
            return;
        }

        setReg(ins.rd(), result);
    }

    private void op_subu(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 rt = getReg(ins.rt());

        UInt32 val = rs - rt;

        setReg(ins.rd(), val);
    }
    
    private void op_bcondz(Instruction ins) {
        UInt32 imm = ins.imm_se();
        Int32 val = (Int32)getReg(ins.rs());

        bool bgez = ((ins.opcode >> 16) & 0x1) == 1;
        bool link = ((ins.opcode >> 20) & 0x1) == 1;

        bool takeBranch = (val < 0 && !bgez) || (val >= 0 && bgez);

        if (link) 
            setReg(31, this.nextPc);
        if (takeBranch) 
            branch(imm);
    }

    private void op_sltiu(Instruction ins) {
        UInt32 imm = ins.imm_se();
        UInt32 rs = getReg(ins.rs());

        UInt32 val = 0;

        if (getReg(rs) < imm)
            val = 1;

        setReg(ins.rt(), val);
    }

    private void op_slti(Instruction ins) {
        Int32 imm = (Int32)ins.imm_se();
        Int32 rs = (Int32)getReg(ins.rs());

        UInt32 val = 0;

        if (rs < imm)
            val = 1;

        setReg(ins.rt(), val);
    }

    private void op_jalr(Instruction ins) {
        UInt32 rs = getReg(ins.rs());

        UInt32 ra = nextPc;

        setReg(ins.rd(), ra);

        this.nextPc = rs;
        this.branchTaken = true;
    }

    private void op_blez(Instruction ins) {
        Int32 rs = (Int32)getReg(ins.rs());

        if (rs <= 0) 
            branch(ins.imm_se());
    }

    private void op_bgtz(Instruction ins) {
        Int32 rs = (Int32)getReg(ins.rs());

        if (rs > 0) 
            branch(ins.imm_se());
    }

    private void op_and(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 rt = getReg(ins.rt());

        UInt32 val = rs & rt;

        setReg(ins.rd(), val);
    }

    private void op_beq(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 rt = getReg(ins.rt());

        if (getReg(rs) == getReg(rt)) 
            branch(ins.imm_se());
    }

    private void op_jal(Instruction ins) {
        UInt32 ra = this.nextPc;
        setReg(31, ra);
        op_j(ins);
    }

    private void op_jr(Instruction ins) {
        this.nextPc = getReg(ins.rs());
        this.branchTaken = true;
    }

    private void op_add(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 rt = getReg(ins.rt());

        UInt32 val = rs + rt;

        if (checkOverflow(rs, rt, val)) {
            triggerException(Except.Overflow);
        }

        setReg(ins.rd(), val);
    }

    private void op_addu(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 rt = getReg(ins.rt());

        UInt32 val = rs + rt;

        setReg(ins.rd(), val);
    }

    private void op_lui(Instruction ins) {
        UInt32 imm = ins.imm();

        UInt32 val = imm << 16;

        setReg(ins.rt(), val);
    }

    private void op_andi(Instruction ins) {
        UInt32 imm = ins.imm();
        UInt32 rs = getReg(ins.rs());

        UInt32 val = getReg(rs) & imm;

        setReg(ins.rt(), val);
    }

    private void op_ori(Instruction ins) {
        UInt32 imm = ins.imm();
        UInt32 rs = getReg(ins.rs());

        UInt32 val = getReg(rs) | imm;

        setReg(ins.rt(), val);
    }

    private void op_lb(Instruction ins) {
        if ((sr & 0x10000) != 0) {
            hub.log.writeLog("Ignoring lb, cache is isolated");
            return;
        }

        UInt32 imm = ins.imm_se();
        UInt32 rs = getReg(ins.rs());

        UInt32 addr = rs + imm;

        UInt32 val = (UInt32)(sbyte)load8(addr);

        this.load = new NextLoad(ins.rt(), val);
    }

    private void op_lh(Instruction ins) {
        UInt32 imm = ins.imm_se();
        UInt32 rs = getReg(ins.rs());

        UInt32 addr = rs + imm;

        if (addr % 2 != 0) {
            triggerException(Except.LoadAddressError);
            return;
        }

        UInt32 val = (UInt32)(Int16)load16(addr);

        this.load = new NextLoad(ins.rt(), val);
    }

    private void op_lhu(Instruction ins) {
        UInt32 imm = ins.imm_se();
        UInt32 rs = getReg(ins.rs());

        UInt32 addr = rs + imm;

        if (addr % 2 != 0) {
            triggerException(Except.LoadAddressError);
            return;
        }

        UInt32 val = load16(addr);

        this.load = new NextLoad(ins.rt(), val);
    }

    private void op_lbu(Instruction ins) {
        if ((sr & 0x10000) != 0) {
            hub.log.writeLog("Ignoring lbu, cache is isolated");
            return;
        }

        UInt32 imm = ins.imm_se();
        UInt32 rs = getReg(ins.rs());

        UInt32 addr = rs + imm;

        UInt32 val = load8(addr);

        this.load = new NextLoad(ins.rt(), val);
    }

    private void op_sb(Instruction ins) {
        if ((sr & 0x10000) != 0) {
            hub.log.writeLog("Ignoring sb, cache is isolated");
            return;
        }

        UInt32 imm = ins.imm_se();
        UInt32 rs = getReg(ins.rs());
        UInt32 val = getReg(ins.rt());

        UInt32 addr = rs + imm;

        store8(addr, (byte)val);
    }

    private void op_sw(Instruction ins) {
        if ((sr & 0x10000) != 0) {
            // cache is isolated, ignore write
            hub.log.writeLog("Ignoring sw, cache is isolated");
            return;
        }

        UInt32 imm = ins.imm_se();
        UInt32 rs = getReg(ins.rs());
        UInt32 val = getReg(ins.rt());

        UInt32 addr = rs + imm;

        if (addr % 4 != 0) {
            triggerException(Except.StoreAddressError);
            return;
        }

        store32(addr, val);
    }

    private void op_sll(Instruction ins) {
        Int32 shamt = (Int32)ins.shamt();
        UInt32 rt = getReg(ins.rt());

        UInt32 val = rt << shamt;

        setReg(ins.rd(), val);
    }

    private void op_addi(Instruction ins) {
        UInt32 imm = ins.imm_se();
        UInt32 rs = getReg(ins.rs());

        UInt32 val = rs + imm;

        if (checkOverflow(getReg(rs), imm, val)) {
            triggerException(Except.Overflow);
        }

        setReg(ins.rt(), val);
    }

    private void op_addiu(Instruction ins) {
        UInt32 imm = ins.imm_se();
        UInt32 rs = getReg(ins.rs());

        UInt32 val = rs + imm;

        setReg(ins.rt(), val);
    }

    private void op_j(Instruction ins) {
        UInt32 pcTemp = this.pc;
        UInt32 addr = ins.tar();

        pcTemp = (pcTemp & 0xF0000000) | (addr << 2);

        this.nextPc = pcTemp;
        this.branchTaken = true;
    }

    private void op_or(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 rt = getReg(ins.rt());
        
        UInt32 val = rs | rt;

        setReg(ins.rd(), val);
    }

    private void op_bne(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 rt = getReg(ins.rt());

        if (getReg(rs) != getReg(rt))
            branch(ins.imm_se());
    }

    private void op_lw(Instruction ins) {
        if ((sr & 0x10000) != 0) {
            hub.log.writeLog("Cache isolated, ignoring lw");
            return;
        }

        UInt32 imm = ins.imm_se();
        UInt32 rs = getReg(ins.rs());

        UInt32 addr = rs + imm;

        if (addr % 4 != 0) {
            triggerException(Except.LoadAddressError);
            return;
        }

        UInt32 val = load32(addr);
        this.load = new NextLoad(ins.rt(), val);
    }

    private void op_slt(Instruction ins) {
        Int32 rs = (Int32)getReg(ins.rs());
        Int32 rt = (Int32)getReg(ins.rt());

        UInt32 val = 0;
        if (rs < rt) {
            val = 1;
        }

        setReg(ins.rd(), val);
    }

    private void op_sltu(Instruction ins) {
        UInt32 rs = getReg(ins.rs());
        UInt32 rt = getReg(ins.rt());

        UInt32 val = 0;
        if (rs < rt) {
            val = 1;
        }

        setReg(ins.rd(), val);
    }

    private void op_sh(Instruction ins) {
        if ((sr & 0x10000) != 0) {
            hub.log.writeLog("Ignoring sh, cache is isolated");
            return;
        }

        UInt32 imm = ins.imm_se();
        UInt32 rs = getReg(ins.rs());
        UInt32 rt = getReg(ins.rt());

        UInt32 addr = rs + imm;
        UInt16 val = (UInt16)rt;

        if (addr % 2 != 0) {
            triggerException(Except.StoreAddressError);
            return;
        }

        store16(addr, val);
    }

    // COP0 opcodes
    private void op_mtc0(Instruction ins) {
        UInt32 copReg = ins.rd();
        UInt32 val = getReg(ins.rt());

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

    private void op_mfc0(Instruction ins) {
        UInt32 copReg = ins.rd();

        UInt32 val = 0;

        switch (copReg) {
            case 12:
                val = this.sr; break;
            case 13:
                val = this.cause; break;
            case 14:
                val = this.epc; break;
            default:
                throw new Exception($"Unhandled read from COP0 Register: {copReg}");
        }

        load = new NextLoad(ins.rt(), val);
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