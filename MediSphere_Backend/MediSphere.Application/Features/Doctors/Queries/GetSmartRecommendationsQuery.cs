using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediSphere.Application.DTOs.Doctor;
using MediSphere.Application.Interfaces;

namespace MediSphere.Application.Features.Doctors.Queries;

public class GetSmartRecommendationsQuery : IRequest<IEnumerable<DoctorDto>>
{
    public string Symptoms { get; set; } = string.Empty;

    public GetSmartRecommendationsQuery(string symptoms)
    {
        Symptoms = symptoms;
    }
}

public class GetSmartRecommendationsQueryHandler : IRequestHandler<GetSmartRecommendationsQuery, IEnumerable<DoctorDto>>
{
    private readonly ISmartRecommendationService _recommendationService;

    public GetSmartRecommendationsQueryHandler(ISmartRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    public async Task<IEnumerable<DoctorDto>> Handle(GetSmartRecommendationsQuery request, CancellationToken cancellationToken)
    {
        return await _recommendationService.RecommendDoctorsAsync(request.Symptoms);
    }
}
