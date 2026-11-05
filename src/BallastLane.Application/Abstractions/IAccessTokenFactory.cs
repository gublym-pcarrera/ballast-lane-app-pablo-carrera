using BallastLane.Domain.Users;

namespace BallastLane.Application.Abstractions;

public interface IAccessTokenFactory
{
    AccessToken Create(User user);
}
