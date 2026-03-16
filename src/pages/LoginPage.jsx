import Navbar from "../components/Navbar";

function LoginPage() {
  return (
    <div>

      <Navbar />

      <div className="container">

        <h2>Login</h2>

        <form>

          <input type="email" placeholder="Email" />

          <input type="password" placeholder="Password" />

          <button>Login</button>

        </form>

      </div>

    </div>
  );
}

export default LoginPage;