import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { API_BASE_URL } from '../utils/config.js';

export default function Login() {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState('');
    const navigate = useNavigate();

    useEffect(() => {
        const handleMessage = (event) => {
            // 验证消息来源 - 使用你的后端端口
            if (event.origin !== API_BASE_URL.replace('/api', '')) {
                return;
            }

            if (event.data.type === 'GOOGLE_AUTH_SUCCESS') {
                console.log('Received Google auth success:', event.data.user);

                // 用户信息已通过Cookie认证保存，无需localStorage

                // 跳转到主页
                navigate('/');
            } else if (event.data.type === 'GOOGLE_AUTH_ERROR') {
                console.log('Received Google auth error:', event.data);
                setError(event.data.message || 'Authentication failed');
                setIsSubmitting(false);
            }
        };

        window.addEventListener('message', handleMessage);

        // 清理事件监听器
        return () => {
            window.removeEventListener('message', handleMessage);
        };
    }, [navigate]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        const emailValue = email.trim().toLowerCase();
        if (!emailValue) return setError('Email required');
        if (!password.trim()) return setError('Password required');
        if (!emailValue.endsWith('@aucklanduni.ac.nz'))
            return setError('Please use your university email (@aucklanduni.ac.nz)');

        setIsSubmitting(true);
        setError('');

        try {
            const response = await fetch(`${API_BASE_URL}/auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',           // <<< important (receive cookie + send next time)
                body: JSON.stringify({ email: emailValue, password })
            });

            if (!response.ok) {
                throw new Error('Login failed');
            }

            const data = await response.json();

            // 用户信息已通过Cookie认证保存，无需localStorage

            // Redirect to Homepage after successful login
            navigate('/', { replace: true });
        } catch (err) {
            setError('Login failed. Please check your credentials and try again.');
        } finally {
            setIsSubmitting(false);
        }
    };

    const checkUserStatus = async () => {
        try {
            const response = await fetch(`${API_BASE_URL}/auth/me`, {
                credentials: 'include'
            });
            if (response.ok) {
                // 用户已登录，跳转到主页
                navigate('/');
            }
        } catch (error) {
            console.log('User not authenticated');
        }
    };

    const handleGoogleLogin = () => {
        setIsSubmitting(true);
        setError(''); // 清除之前的错误

        // 标记是否收到了认证结果消息
        let receivedAuthResult = false;

        // 临时消息处理器，用于检测是否收到了认证结果
        const tempMessageHandler = (event) => {
            if (event.origin !== API_BASE_URL.replace('/api', '')) {
                return;
            }
            if (event.data.type === 'GOOGLE_AUTH_SUCCESS' || event.data.type === 'GOOGLE_AUTH_ERROR') {
                receivedAuthResult = true;
            }
        };

        // 添加临时消息监听器
        window.addEventListener('message', tempMessageHandler);

        // 打开弹窗进行Google OAuth认证
        const popup = window.open(
            `${API_BASE_URL}/auth/google?returnUrl=${encodeURIComponent('/events')}`,
            'google-login',
            'width=500,height=600,scrollbars=yes,resizable=yes'
        );

        // 检查弹窗是否被阻止
        if (!popup) {
            setError('Popup blocked. Please allow popups for this site.');
            setIsSubmitting(false);
            window.removeEventListener('message', tempMessageHandler);
            return;
        }

        // 监听弹窗关闭
        const checkClosed = setInterval(() => {
            if (popup.closed) {
                clearInterval(checkClosed);
                window.removeEventListener('message', tempMessageHandler);
                setIsSubmitting(false);
                
                // 只有在没有收到认证结果消息的情况下才检查用户状态
                if (!receivedAuthResult) {
                    setTimeout(() => {
                        checkUserStatus();
                    }, 500);
                }
            }
        }, 1000);
    };

    // 检查URL中的错误参数（如果用户直接访问回调URL后被重定向）
    useEffect(() => {
        const urlParams = new URLSearchParams(window.location.search);
        const error = urlParams.get('error');

        if (error) {
            switch (error) {
                case 'google_failed':
                    setError('Google authentication failed. Please try again.');
                    break;
                case 'missing_info':
                    setError('Missing information from Google. Please try again.');
                    break;
                case 'invalid_domain':
                    setError('Please use your Auckland University email (@aucklanduni.ac.nz).');
                    break;
                case 'user_creation_failed':
                    setError('Failed to create user account. Please contact support.');
                    break;
                case 'callback_error':
                    setError('Authentication error occurred. Please try again.');
                    break;
                default:
                    setError('An error occurred during authentication. Please try again.');
            }
        }
    }, []);

    return (
        <main className="container" style={{ marginTop: '20px' }}>
            <section className="card">
                <h2 style={{ margin: '0 0 8px' }}>Log in</h2>
                <p className="help">Use your university email to access the campus event system.</p >

                {error && (
                    <div style={{
                        background: '#fef2f2',
                        color: '#b91c1c',
                        border: '1px solid #fecaca',
                        borderRadius: '12px',
                        padding: '10px',
                        marginBottom: '16px',
                        fontSize: '14px'
                    }}>
                        {error}
                    </div>
                )}

                {/* Google OAuth Login Button */}
                <div style={{ marginBottom: '20px' }}>
                    <button
                        onClick={handleGoogleLogin}
                        disabled={isSubmitting}
                        style={{
                            width: '100%',
                            padding: '12px 16px',
                            border: '1px solid #dadce0',
                            borderRadius: '16px',
                            background: '#fff',
                            color: '#3c4043',
                            fontSize: '14px',
                            fontWeight: '500',
                            cursor: isSubmitting ? 'not-allowed' : 'pointer',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            gap: '10px',
                            transition: 'background 0.2s, box-shadow 0.2s',
                            opacity: isSubmitting ? 0.7 : 1
                        }}
                        onMouseEnter={(e) => {
                            if (!isSubmitting) {
                                e.target.style.background = '#f8f9fa';
                                e.target.style.boxShadow = '0 2px 8px rgba(0,0,0,0.1)';
                            }
                        }}
                        onMouseLeave={(e) => {
                            if (!isSubmitting) {
                                e.target.style.background = '#fff';
                                e.target.style.boxShadow = 'none';
                            }
                        }}
                    >
                        <svg width="18" height="18" viewBox="0 0 24 24">
                            <path fill="#4285f4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
                            <path fill="#34a853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
                            <path fill="#fbbc05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
                            <path fill="#ea4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
                        </svg>
                        {isSubmitting ? 'Signing in...' : 'Continue with Google'}
                    </button>
                </div>

                <div style={{
                    display: 'flex',
                    alignItems: 'center',
                    margin: '20px 0',
                    color: '#6b7280'
                }}>
                    <div style={{ flex: 1, height: '1px', background: '#e5e7eb' }}></div>
                    <div style={{ flex: 1, height: '1px', background: '#e5e7eb' }}></div>
                </div>
            </section>
        </main>
    );
}