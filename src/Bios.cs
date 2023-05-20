public class Bios {
    const int BIOS_SIZE = 512 * 1024;
    const uint BIOS_OFFSET = 0xbfc00000;
    readonly uint[] BIOS_RANGE = new uint[]{ 0xbfc00000, 0xbfc00000 + (512 * 1024) };

    public byte[] data;

    public Bios() {
        string filePath = "./bios/SCPH1001.BIN";

        data = File.ReadAllBytes(filePath);

        if (data.Length == BIOS_SIZE) {
            Console.WriteLine("Bios size OK");
        } else {
            Console.WriteLine("Bios invalid size: " + data.Length);
        }
    }

    public UInt32 contains(UInt32 addr) {
        if (addr >= BIOS_RANGE[0] && addr < BIOS_RANGE[1]) {
            return addr - BIOS_RANGE[0];
        }
        return 0xFFFFFFFF;
    }

    public UInt32 load32(UInt32 offset) {
        Console.WriteLine("{0:x}", offset);

        UInt32 b0 = this.data[offset + 0];
        UInt32 b1 = this.data[offset + 1];
        UInt32 b2 = this.data[offset + 2];
        UInt32 b3 = this.data[offset + 3];

        return b0 | (b1 << 8) | (b2 << 16) | (b3 << 24);
    }
}