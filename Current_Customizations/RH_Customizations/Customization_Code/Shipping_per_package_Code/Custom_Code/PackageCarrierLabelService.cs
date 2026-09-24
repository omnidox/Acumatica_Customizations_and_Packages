using System;
using System.Linq;
using System.Collections.Generic;
using PX.Data;
using PX.Objects.SO;
using PX.SM;
using PX.CarrierService;
using PX.Objects.CS;

namespace PX.Objects.SO
{
    public class PackageCarrierLabelService
    {
        private readonly SOShipmentEntry _graph;

        public PackageCarrierLabelService(SOShipmentEntry graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public virtual void ValidatePackageForGeneration(SOShipment shipment, SOPackageDetailEx package)
        {
            if (shipment == null)
                throw new PXException("No shipment is currently selected.");

            if (package == null)
                throw new PXException("No package is currently selected on the Packages tab.");

            if (package.LineNbr == null)
                throw new PXException("The selected package does not have a valid line number.");

            if (string.IsNullOrWhiteSpace(shipment.ShipVia))
                throw new PXException("Ship Via is required before generating a package label.");

            if (package.Weight == null || package.Weight <= 0m)
                throw new PXException("Package weight must be greater than zero before generating a label.");
        }

        public class PreservedPackageTracking
        {
            public int? LineNbr { get; set; }
            public string TrackNumber { get; set; }
            public string TrackUrl { get; set; }
        
            // If this type does not compile in your environment,
            // change it to match SOPackageDetailEx.TrackData's actual type.
            public string TrackData { get; set; }
        }

          
        public virtual Dictionary<int, PreservedPackageTracking> CaptureTrackingForPackagesWithExistingLabels(string shipmentNbr)
        {
            var preserved = new Dictionary<int, PreservedPackageTracking>();
        
            if (string.IsNullOrWhiteSpace(shipmentNbr))
                return preserved;
        
            foreach (SOPackageDetailEx package in PXSelect<
                SOPackageDetailEx,
                Where<SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>>>
                .Select(_graph, shipmentNbr))
            {
                if (package?.LineNbr == null)
                    continue;
        
                FileInfo existingLabel = TryGetExistingCarrierLabel(package);
                if (existingLabel == null)
                    continue;
        
                if (string.IsNullOrWhiteSpace(package.TrackNumber))
                    continue;
        
                preserved[package.LineNbr.Value] = new PreservedPackageTracking
                {
                    LineNbr = package.LineNbr,
                    TrackNumber = package.TrackNumber,
                    TrackUrl = package.TrackUrl,
                    TrackData = package.TrackData
                };
            }
        
            return preserved;
        }
          
        public virtual void RestoreTrackingForPackages(string shipmentNbr, Dictionary<int, PreservedPackageTracking> preserved)
        {
            if (string.IsNullOrWhiteSpace(shipmentNbr) || preserved == null || preserved.Count == 0)
                return;
        
            foreach (SOPackageDetailEx package in PXSelect<
                SOPackageDetailEx,
                Where<SOPackageDetailEx.shipmentNbr, Equal<Required<SOPackageDetailEx.shipmentNbr>>>>
                .Select(_graph, shipmentNbr))
            {
                if (package?.LineNbr == null)
                    continue;
        
                if (!preserved.TryGetValue(package.LineNbr.Value, out PreservedPackageTracking snapshot))
                    continue;
        
                package.TrackNumber = snapshot.TrackNumber;
                package.TrackUrl = snapshot.TrackUrl;
                package.TrackData = snapshot.TrackData;
        
                _graph.Packages.Update(package);
            }
        }
          
        public virtual FileInfo TryGetExistingCarrierLabel(SOPackageDetailEx package)
        {
            if (package == null)
                return null;

            Guid[] fileNotes = PXNoteAttribute.GetFileNotes(_graph.Packages.Cache, package);
            if (fileNotes == null || fileNotes.Length == 0)
                return null;

            UploadFileMaintenance upload = PXGraph.CreateInstance<UploadFileMaintenance>();
            string[] allowed = { ".zpl", ".zplii", ".epl", ".pdf" };

            return fileNotes
                .Select(id => upload.GetFile(id))
                .Where(f => f != null && !string.IsNullOrEmpty(f.Name))
                .FirstOrDefault(f => allowed.Any(ext => f.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
        }

        public virtual FileInfo GenerateCarrierLabelForPackage(SOShipment shipment, SOPackageDetailEx package)
        {
            ValidatePackageForGeneration(shipment, package);

            ICarrierService cs = CarrierMaint.CreateCarrierService(_graph, shipment.ShipVia);
            if (cs == null)
                throw new PXException($"Carrier service could not be created for Ship Via '{shipment.ShipVia}'.");

            // This assumes your graph extension/method exists in your environment.
            CarrierRequest cr = _graph.CarrierRatesExt.BuildRequest(shipment);

            if (cr == null)
                throw new PXException("Carrier request could not be created.");

            if (cr.Packages == null || cr.Packages.Count == 0)
                throw new PXException("The carrier request does not contain any packages.");

            var selectedCarrierPackage = cr.Packages
                .FirstOrDefault(p => object.Equals(p.RefNbr, package.LineNbr));

            if (selectedCarrierPackage == null)
                throw new PXException($"Could not match selected package line {package.LineNbr} to the carrier request packages.");

            cr.Packages = new List<CarrierBox> { selectedCarrierPackage };

            var result = cs.Ship(cr);

            if (result == null)
                throw new PXException("Carrier returned no result.");

            if (!result.IsSuccess)
            {
                string errors =
                    result.Messages == null || result.Messages.Count == 0
                        ? "Unknown carrier error."
                        : string.Join(" ", result.Messages.Select(m => $"{m.Code}:{m.Description}"));

                throw new PXException(
                    $"Carrier generation failed for package line {package.LineNbr}. {errors}"
                );
            }

            if (result.Result?.Data == null || result.Result.Data.Count == 0)
            {
                throw new PXException(
                    $"Carrier returned success but no package label data for package line {package.LineNbr}."
                );
            }

            var packageData =
                result.Result.Data.FirstOrDefault(d => object.Equals(d.RefNbr, package.LineNbr))
                ?? result.Result.Data.FirstOrDefault();

            if (packageData == null)
            {
                throw new PXException(
                    $"Carrier returned no matching package result for package line {package.LineNbr}."
                );
            }

            if (packageData.Image == null || packageData.Image.Length == 0)
            {
                throw new PXException(
                    $"Carrier returned no label image for package line {package.LineNbr}."
                );
            }

            string trackingNumber = string.IsNullOrWhiteSpace(packageData.TrackingNumber)
                ? $"PackageLine{package.LineNbr}"
                : packageData.TrackingNumber;

            string extension = string.IsNullOrWhiteSpace(packageData.Format)
                ? "pdf"
                : packageData.Format.Trim().TrimStart('.');

            string fileName = $"Label #{trackingNumber}.{extension}";

            UploadFileMaintenance upload = PXGraph.CreateInstance<UploadFileMaintenance>();
            FileInfo file = new FileInfo(fileName, null, packageData.Image);

            if (!upload.SaveFile(file))
                throw new PXException("Carrier label file could not be saved.");

            AttachLabelFileToPackage(package, file);

            package.TrackNumber = packageData.TrackingNumber;
            package.TrackUrl = packageData.TrackingUrl;
            package.TrackData = packageData.TrackingData;
            
            _graph.Packages.Update(package);
            _graph.Actions.PressSave();

            return file;
        }

        public virtual void AttachLabelFileToPackage(SOPackageDetailEx package, FileInfo fileInfo)
        {
            if (package == null)
                throw new PXException("Package is required.");

            if (fileInfo?.UID == null)
                throw new PXException("A saved file is required.");

            PXNoteAttribute.SetFileNotes(_graph.Packages.Cache, package, fileInfo.UID.Value);
            _graph.Packages.Update(package);
        }

        public virtual void PrintSingleFile(FileInfo fileInfo)
        {
            if (fileInfo == null)
                throw new PXException("No label file was provided for printing.");

            throw new PXRedirectToFileException(fileInfo, true);
        }
    }
}