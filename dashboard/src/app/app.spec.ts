describe('Jest Setup', () => {
  it('should run tests', () => {
    expect(true).toBe(true);
  });

  it('should have jest-dom matchers', () => {
    const div = document.createElement('div');
    div.innerHTML = 'Hello';
    document.body.appendChild(div);
    expect(div).toBeInTheDocument();
  });
});



