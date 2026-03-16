import Navbar from "../components/Navbar";

function RegisterPage() {
  return (
    <div>

      <Navbar />

      <div className="container">

        <h2>Create Account</h2>

        <form>

          <input placeholder="Username" />

          <input type="email" placeholder="Email" />

          <input type="password" placeholder="Password" />

          <button>Register</button>

        </form>

      </div>

    </div>
  );
}

export default RegisterPage;