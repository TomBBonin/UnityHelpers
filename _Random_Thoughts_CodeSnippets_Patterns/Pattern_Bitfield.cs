/*
 * This shows you how to use a Bitfield in C#
 * It's basically a way of storing a ton of flags into a single int
 * For example, if a player can earn 20 different medals. Rather than
 * having 20 bools set to true or false, you can just use a Bitfield and
 * save yourself a lot of time, space and miles of code.
 * 
 * The syntax is a bit tricky but if you understand Bitwise operations, you'll be fine!
 * https://en.wikipedia.org/wiki/Bitwise_operation
 * 
 * https://github.com/tombbonin
 */

public class BitifieldPattern 
{
    // This is how you declare your bitfield, you're assigning values
    // by directly setting the bits to 1 moved to the left by x
    public enum MyBitFieldFlags
    {
        FLAG_0 = 1 << 0,
        FLAG_1 = 1 << 1,
        FLAG_2 = 1 << 2,
        FLAG_3 = 1 << 3,
        FLAG_4 = 1 << 4,
        FLAG_5 = 1 << 5
    }

    public int Bitfield;

    // Checks your field to see if a flag was set
    public bool IsFlagSet(MyBitFieldFlags flag)
    {
        return (Bitfield & (int)flag) != 0;
    }

    // Sets a flag in your field
    public void SetFlag(MyBitFieldFlags flag)
    {
        Bitfield |= (int)flag;
    }

    // removes a flag from the field
    public void RemoveFlag(MyBitFieldFlags flag)
    {
        Bitfield &= (int)~flag;
    }
}
