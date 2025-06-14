function filterCars() {
    const selectedVehicleTypes = getCheckedValues('SelectedVehicleTypeIds');
    const selectedBodyTypes = getCheckedValues('SelectedBodyTypeIds');
    const selectedSeats = getCheckedValues('SelectedSeatCounts');
    const selectedEngineRanges = getCheckedValues('SelectedEngineRanges');

    const minInput = document.querySelector('[name="MinPrice"]');
    const maxInput = document.querySelector('[name="MaxPrice"]');
    const minPrice = minInput && minInput.value !== "" ? parseInt(minInput.value) : 0;
    const maxPrice = maxInput && maxInput.value !== "" ? parseInt(maxInput.value) : 999999;

    const container = document.querySelector('#car-list');
    const allCards = Array.from(container.querySelectorAll('.car-card'));

    // Clear container
    container.innerHTML = '';

    // Filter and reorder
    allCards.forEach(card => {
        const vehicleType = parseInt(card.dataset.vehicleType);
        const bodyType = parseInt(card.dataset.bodyType);
        const seats = parseInt(card.dataset.seats);
        const engine = parseInt(card.dataset.engine);
        const price = parseInt(card.dataset.price);

        const visible =
            (selectedVehicleTypes.length === 0 || selectedVehicleTypes.includes(vehicleType)) &&
            (selectedBodyTypes.length === 0 || selectedBodyTypes.includes(bodyType)) &&
            (selectedSeats.length === 0 || selectedSeats.includes(seats)) &&
            (selectedEngineRanges.length === 0 || selectedEngineRanges.some(range => {
                const [min, max] = range.split('-');
                return engine >= parseInt(min) && engine <= (max === "max" ? Infinity : parseInt(max));
            })) &&
            (price >= minPrice && price <= maxPrice);

        if (visible) {
            container.appendChild(card); // Append to the end in new order
        }
    });
}
