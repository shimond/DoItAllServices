using WebClientBffGateway.Models;

namespace WebClientBffGateway.Models.ComblexModels
{
    public record FullPatientCurrentStatusData(PatientBasicInfoModel Patinet, VitalsData Vitals);
}
