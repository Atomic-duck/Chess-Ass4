using Unity.Networking.Transport;

public class NetStartGame : NetMessage
{
    public int AssignedTeam { get; set; }
    public NetStartGame()
    {
        code = OpCode.START_GAME;
    }
    public NetStartGame(DataStreamReader reader)
    {
        code = OpCode.START_GAME;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)code);
    }
    public override void Deserialize(DataStreamReader reader)
    {
       
    }
    public override void ReceivedOnClient()
    {
        NetUtility.C_START_GAME?.Invoke(this);
    }
    public override void ReceivedOnServer(NetworkConnection cnn)
    {
        NetUtility.S_START_GAME?.Invoke(this, cnn);
    }
}

