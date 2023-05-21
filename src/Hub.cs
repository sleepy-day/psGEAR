public class Hub {
    readonly UInt32[] RAM = new UInt32[]{ 0x00000000, 2 * 1024 * 1024 };
    readonly UInt32[] BIOS = new UInt32[]{ 0x1FC00000, 512 * 1024 };
    readonly UInt32[] SYS_CONTROL = new UInt32[]{ 0x1F801000, 36 };
    readonly UInt32[] RAM_SIZE = new UInt32[]{ 0x1F801060, 4 };
    readonly UInt32[] CACHE_CONTROL = new UInt32[]{ 0xFFFE0130, 4 };
    readonly UInt32[] MEM_CONTROL = new UInt32[]{ 0x1F801000, 36 };
    readonly UInt32[] SPU = new UInt32[]{ 0x1F801C00, 640 };
    readonly UInt32[] EXPANSION_2 = new UInt32[]{ 0x1F802000, 66 };
    readonly UInt32[] EXPANSION_1 = new UInt32[]{ 0x1F000000, 8 * 1024 * 1024 };
    readonly UInt32[] REGION_MASK = new UInt32[]{
        // KUSEG: 2048MB
        0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
        // KSEG0: 512MB
        0x7FFFFFFF,
        // KSEG1: 512MB
        0x1FFFFFFF,
        // KSEG2: 1024MB
        0xFFFFFFFF, 0xFFFFFFFF
    };

    Bios bios;
    Ram ram;

    public Hub() {
        this.bios = new Bios();
        this.ram = new Ram();
    }

    public UInt32 maskRegion(UInt32 addr) {
        UInt32 index = addr >> 29;

        return addr & REGION_MASK[index];
    }

    private (UInt32 offset, bool result) contains(UInt32 addr, UInt32[] region) {
        addr = maskRegion(addr);
        UInt32 start = region[0];
        UInt32 length = region[1];

        if (addr >= start && addr < start + length) {
            addr -= start;
            return (addr, true);
        }

        return (addr, false);
    }

    public byte load8(UInt32 addr) {
        var c = contains(addr, BIOS);
        if (c.result) {
            return bios.load8(c.offset);
        }

        c = contains(addr, EXPANSION_1);
        if (c.result) {
            // expansion slot 1, currently not implemented
            return 0xFF;
        }

        c = contains(addr, RAM);
        if (c.result) {
            return ram.load8(c.offset);
        }

        throw new Exception($"Unhandled load8 at address: {addr:X}");
    }

    public UInt32 load32(UInt32 addr) {
        var c = contains(addr, BIOS);
        if (c.result) {
            return bios.load32(c.offset);
        }

        c = contains(addr, RAM);
        if (c.result) {
            return ram.load32(c.offset);
        }

        throw new Exception($"Unhandled load32 at address: {addr:X}");
    }

    public void store8(UInt32 addr, byte val) {
        var c = contains(addr, EXPANSION_2);
        if (c.result) {
            Console.WriteLine("Write to expansion 2 reg");
            return;
        }

        c = contains(addr, RAM);
        if (c.result) {
            ram.store8(c.offset, val);
        }

        throw new Exception($"Unhandled store8 into address {addr:X}");
    }

    public void store16(UInt32 addr, UInt16 val) {
        if (addr % 2 != 0) {
            throw new Exception($"Unaligned store16 address: {addr:X}");
        }

        var c = contains(addr, SPU);
        if (c.result) {
            Console.WriteLine("Write to SPU");
            return;
        }

        throw new Exception($"Unhandled store16 address: {addr:X} val: {val:X}");
    }

    public void store32(UInt32 addr) {
        if (addr % 4 != 0) {
            Console.WriteLine("Unaligned load32 address: {0:x}", addr);
            throw new Exception("Terminating due to unaligned load32 address");
        }
        Console.WriteLine("Unaligned load32 address: {0:x}", addr);
        throw new Exception("Terminating due to unaligned load32 address");
    }

    public void store32(UInt32 addr, UInt32 val) {
        if (addr % 4 != 0) {
            Console.WriteLine("Unaligned load32 address: {0:x}", addr);
            throw new Exception("Terminating due to unaligned load32 address");
        }

        Console.WriteLine($"Writing value: {val:X} at address: {addr:X}");

        var c = contains(addr, RAM);
        if (c.result) {
            ram.store32(c.offset, val);
            return;
        }

        c = contains(addr, MEM_CONTROL);
        if (c.result) {
            switch (c.offset) {
                case 0x1F801000:
                    if (val != 0x1F000000) {
                        Console.WriteLine("Invalid expansion 1 base address: {0:x}", val);
                        throw new Exception("Terminating");
                    } break;
                case 0x1F801004:
                    if (val != 0x1F802000) {
                        Console.WriteLine("Invalid expansion 2 base address: {0:x}", val);
                        throw new Exception("Terminating");
                    } break;
                default:
                    Console.WriteLine("Unhandled write to MEMCONTROL register"); break;
            }
            return;
        }

        c = contains(addr, SYS_CONTROL);
        if (c.result) {
            Console.WriteLine("SYS CONTROL register write");
            return;
        }

        c = contains(addr, RAM_SIZE);
        if (c.result) {
            // RAM configuration
            Console.WriteLine("Ram Configuration");
            return;
        }

        c = contains(addr, CACHE_CONTROL);
        if (c.result) {
            // Cache control
            Console.WriteLine("Cache control configuration");
            return;
        }

        Console.WriteLine("Unhandled store32 into address: {0:x}", addr);
        throw new Exception("Terminating");    
    }
}