namespace BallastLane.Api.Security;

public interface ICurrentUser
{
    Guid UserId { get; }
}
