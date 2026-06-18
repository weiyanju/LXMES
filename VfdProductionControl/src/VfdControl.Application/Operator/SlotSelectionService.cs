using VfdControl.Application.Common;
using VfdControl.Application.Execution;
using VfdControl.Domain.Stations;
using VfdControl.Domain.ValueObjects;

namespace VfdControl.Application.Operator;

public sealed class SlotSelectionService
{
    public AppResult<SlotScanQueue> CreateScanQueue(Station station, IReadOnlyList<int> selectedSlotNumbers)
    {
        var selectedSlots = selectedSlotNumbers
            .Select(number => station.Slots.SingleOrDefault(slot => slot.Number.Value == number))
            .ToList();

        if (selectedSlots.Any(slot => slot is null))
        {
            return AppResult<SlotScanQueue>.Failure("Selected slot does not exist.", "SlotSelection.NotFound");
        }

        return AppResult<SlotScanQueue>.Success(new SlotScanQueue(selectedSlots!));
    }

    public AppResult<SlotBarcodeBindingSet> BindBarcodes(SlotScanQueue scanQueue, IReadOnlyList<string> barcodes)
    {
        if (barcodes.Count != scanQueue.Slots.Count)
        {
            return AppResult<SlotBarcodeBindingSet>.Failure("Barcode count must match selected slot count.", "SlotSelection.BarcodeCountMismatch");
        }

        var bindings = new List<SlotBarcodeBinding>();
        for (var index = 0; index < scanQueue.Slots.Count; index++)
        {
            var barcode = Barcode.TryCreateVfd(barcodes[index]);
            if (!barcode.IsSuccess || barcode.Value is null)
            {
                return AppResult<SlotBarcodeBindingSet>.Failure(barcode.Error?.Message ?? "Invalid VFD barcode.", barcode.Error?.Code);
            }

            bindings.Add(new SlotBarcodeBinding(scanQueue.Slots[index], barcode.Value));
        }

        return AppResult<SlotBarcodeBindingSet>.Success(new SlotBarcodeBindingSet(bindings));
    }
}

public sealed record SlotScanQueue(IReadOnlyList<StationSlot> Slots);

public sealed record SlotBarcodeBindingSet(IReadOnlyList<SlotBarcodeBinding> Bindings);
