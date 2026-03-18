import Navbar from "../components/Navbar";

function HomePage() {
  return (
    <div>

      <Navbar />

      <div className="container">

        <h1>Find Your Mentor</h1>

        <input placeholder="Search mentor by subject..." />
        <h2>Recommended Mentors</h2>
        <div className="mentor-list">

          <div className="mentor-card">
            <h3>Ivan Petrov</h3>
            <p>Math Mentor</p>
            <p>Rate: 4.8 / 5</p>
          </div>

          <div className="mentor-card">
            <h3>Maria Dimitrova</h3>
            <p>English Mentor</p>
            <p>Rate: 4.6 / 5</p>
          </div>

        </div>

      </div>

    </div>
  );
}

export default HomePage;