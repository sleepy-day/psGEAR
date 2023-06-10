public class Ram {
    public byte[] ram;
    readonly public UInt32[] MEMORY_RANGE = new uint[]{ 0xA0000000, 0xA0000000 + (2 * 1024 * 1024) };

    public Ram() {
        this.ram = new byte[2 * 1024 * 1024];
        Array.Fill<byte>(this.ram, 0x00FB);
    }

    public byte load8(UInt32 offset) => ram[offset];

    public UInt16 load16(UInt32 offset) {
        UInt32 b0 = this.ram[offset + 0];
        UInt32 b1 = this.ram[offset + 1];

        UInt32 val = b0 | (b1 << 8);

        return (UInt16)(val & 0xFFFF);
    }

    public UInt32 load32(UInt32 offset) {
        Console.WriteLine("{0:x}", offset);

        UInt32 b0 = this.ram[offset + 0];
        UInt32 b1 = this.ram[offset + 1];
        UInt32 b2 = this.ram[offset + 2];
        UInt32 b3 = this.ram[offset + 3];

        return b0 | (b1 << 8) | (b2 << 16) | (b3 << 24);
    }

    public void store8(UInt32 offset, byte val) => ram[offset] = val;

    public void store16(UInt32 offset, UInt16 val) {
        byte b0 = (byte)val;
        byte b1 = (byte)(val >> 8);

        this.ram[offset + 0] = b0;
        this.ram[offset + 1] = b1;
    }

    public void store32(UInt32 offset, UInt32 val) {
        byte b0 = (byte)val;
        byte b1 = (byte)(val >> 8);
        byte b2 = (byte)(val >> 16);
        byte b3 = (byte)(val >> 24);

        this.ram[offset + 0] = b0;
        this.ram[offset + 1] = b1;
        this.ram[offset + 2] = b2;
        this.ram[offset + 3] = b3;
    } 

    public UInt32 contains(UInt32 addr) {
        if (addr >= MEMORY_RANGE[0] && addr < MEMORY_RANGE[1]) {
            return addr - MEMORY_RANGE[0];
        }
        return 0xFFFFFFFF;
    }
}